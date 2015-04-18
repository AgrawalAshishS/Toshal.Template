using System;
using System.Collections.Generic;
using Template;
using Template.Exceptions;
using Template.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Template
{
	[TestClass]
	public class TemplateParserTests
	{
		//TODO: Add For each tests for multiple occurrence of inner elements.

		[TestMethod]
		public void ItShouldHandleSimpleText()
		{
			const string templateText = "Simple text";
			var parser = new Parser();

			List<IToken> result = parser.Parse(templateText);
			Assert.AreEqual(1, result.Count);
			Assert.IsInstanceOfType(result[0], typeof (ContentToken));
			Assert.AreEqual(templateText, ((ContentToken) result[0]).Content);
		}

		[TestMethod]
		[ExpectedException(typeof (ParserException))]
		public void ItShouldThrowErrorOnUnKnownTag()
		{
			const string templateText = "Simple text<%%>";
			var parser = new Parser();

			try
			{
				parser.Parse(templateText);
			}
			catch (ParserException ex)
			{
				Assert.AreEqual("<%%>", ex.Split);
				throw;
			}
		}

		[TestMethod]
		public void ItShouldHandleBasicToken()
		{
			var parser = new Parser();

			List<IToken> result = parser.Parse("<%=Name%>");
			Assert.AreEqual(1, result.Count);
			Assert.IsInstanceOfType(result[0], typeof (NamedToken));
		}

		[TestMethod]
		public void TokenNameShouldBeLowerCase()
		{
			var parser = new Parser();

			List<IToken> result = parser.Parse("<%=Name%>");
			Assert.AreEqual(1, result.Count);
			Assert.AreEqual("name", ((NamedToken) result[0]).Name);
		}

		[TestMethod]
		[ExpectedException(typeof (TokenMissingNameException))]
		public void TokenNameRequired()
		{
			var parser = new Parser();
			try
			{
				parser.Parse("<%=%>");
			}
			catch (TokenMissingNameException ex)
			{
				Assert.AreEqual("<%=%>", ex.Split);
				throw;
			}
		}

		[TestMethod]
		public void BasicCombinationOfStringAndToken()
		{
			string part1 = "My basic text ";
			string part2 = "<%=";
			string part3 = "Name";
			string part4 = "%>";
			string part5 = " more text for me to work";
			string part6 = "<%=";
			string part7 = "Name2";
			string part8 = "%>";

			string templateText = part1 + part2 + part3 + part4 + part5 + part6 + part7 + part8;
			var parser = new Parser();

			List<IToken> result = parser.Parse(templateText);
			Assert.AreEqual(4, result.Count);
			Assert.AreEqual(part1, ((ContentToken) result[0]).Content);
			Assert.AreEqual(part3.ToLower(), ((NamedToken) result[1]).Name);
			Assert.AreEqual(part5, ((ContentToken) result[2]).Content);
			Assert.AreEqual(part7.ToLower(), ((NamedToken) result[3]).Name);
		}

		[TestMethod]
		public void ItShouldHandleConditionToken()
		{
			string part1 = "<%IF ";
			string part2 = "ConditionName";
			string part3 = " THEN%>";
			string part4 = "Content within IF";
			string part5 = "<%ENDIF%>";

			string templateText = part1 + part2 + part3 + part4 + part5;

			var parser = new Parser();

			List<IToken> result = parser.Parse(templateText);
			Assert.AreEqual(part2.ToLower(), ((ConditionToken) result[0]).Name);
		}

		[TestMethod]
		public void IfConditionWithoutThenIsAlsoOk()
		{
			string part1 = "<%IF ";
			string part2 = "ConditionName";
			string part3 = " %>";
			string part4 = "Content within IF";
			string part5 = "<%ENDIF%>";

			string templateText = part1 + part2 + part3 + part4 + part5;

			var parser = new Parser();

			List<IToken> result = parser.Parse(templateText);
			Assert.AreEqual(part2.ToLower(), ((ConditionToken) result[0]).Name);
		}

		[TestMethod]
		[ExpectedException(typeof (TokenMissingNameException))]
		public void IfWithoutConditionNameShouldThrowException()
		{
			string part1 = "<%IF ";
			string part2 = "";
			string part3 = " %>";
			string part4 = "Content within IF";
			string part5 = "<%ENDIF%>";

			string templateText = part1 + part2 + part3 + part4 + part5;

			var parser = new Parser();

			try
			{
				parser.Parse(templateText);
			}
			catch (TokenMissingNameException ex)
			{
				Assert.AreEqual("<%IF  %>", ex.Split);
				throw;
			}
		}

		[TestMethod]
		public void AllContentBetweenIfToEndIfShouldNotBeInTopResultItShouldBeChildOfIfToken()
		{
			string part1 = "<%IF ";
			string part2 = "ConditionName";
			string part3 = " THEN%>";
			string part4 = "Content within IF";
			string part5 = "<%ENDIF%>";

			string templateText = part1 + part2 + part3 + part4 + part5;

			var parser = new Parser();

			List<IToken> result = parser.Parse(templateText);
			Assert.AreEqual(1, result.Count);

			var token = (ConditionToken) result[0];
			Assert.AreEqual(part2.ToLower(), token.Name);
			Assert.AreEqual(1, token.InnerTokens.Count);

			var content = (ContentToken) token.InnerTokens[0];
			Assert.AreEqual(part4, content.Content);
		}

		[TestMethod]
		[ExpectedException(typeof (TokenNotClosedException))]
		public void IfWithoutEndIfShouldThrowException()
		{
			string part1 = "<%IF ";
			string part2 = "ConditionName";
			string part3 = " THEN%>";
			string part4 = "Content within IF";

			string templateText = part1 + part2 + part3 + part4;

			var parser = new Parser();
			try
			{
				parser.Parse(templateText);
			}
			catch (TokenNotClosedException ex)
			{
				Assert.AreEqual("<%IF ConditionName THEN%>", ex.Split);
				throw;
			}
		}

		[TestMethod]
		[ExpectedException(typeof (TokenNotClosedException))]
		public void IfWithoutEndIfShouldThrowException2()
		{
			string part1 = "<%IF ";
			string part2 = "ConditionName";
			string part3 = " THEN%>";
			string part4 = "Content within IF<%%>";

			string templateText = part1 + part2 + part3 + part4;

			var parser = new Parser();
			try
			{
				parser.Parse(templateText);
			}
			catch (TokenNotClosedException ex)
			{
				Assert.AreEqual("<%IF ConditionName THEN%>", ex.Split);
				throw;
			}
		}

		[TestMethod]
		public void VerifyNestedIfConditionWorksCorrectly()
		{
			string part1 = "<%IF ";
			string part2 = "ConditionName";
			string part3 = " THEN%>";
			string part4 = "<%IF ";
			string part5 = "ConditionName2";
			string part6 = " THEN%>";
			string part7 = "Content within IF";
			string part8 = "<%ENDIF%>";
			string part9 = "<%ENDIF%>";
			string part10 = "other content";

			string templateText = part1 + part2 + part3 + part4 + part5 + part6 + part7 + part8 + part9 + part10;

			var parser = new Parser();

			List<IToken> result = parser.Parse(templateText);
			Assert.AreEqual(2, result.Count);

			var token = (ConditionToken) result[0];
			Assert.AreEqual(part2.ToLower(), token.Name);
			Assert.AreEqual(1, token.InnerTokens.Count);

			var nestedIf = (ConditionToken) token.InnerTokens[0];
			Assert.AreEqual(part5.ToLower(), nestedIf.Name);
			Assert.AreEqual(1, nestedIf.InnerTokens.Count);

			var content = (ContentToken) nestedIf.InnerTokens[0];
			Assert.AreEqual(part7, content.Content);
		}

		[TestMethod]
		[ExpectedException(typeof (TokenNotClosedException))]
		public void VerifyNestedIfConditionWithNotClosingBreaketThrowErrorForParentIf()
		{
			string part1 = "<%IF ";
			string part2 = "ConditionName";
			string part3 = " THEN%>";
			string part4 = "<%IF ";
			string part5 = "ConditionName2";
			string part6 = " THEN%>";
			string part7 = "Content within IF";
			string part8 = "<%ENDIF%>";

			string templateText = part1 + part2 + part3 + part4 + part5 + part6 + part7 + part8;

			var parser = new Parser();

			try
			{
				parser.Parse(templateText);
			}
			catch (TokenNotClosedException ex)
			{
				Assert.AreEqual(part2.ToLower(), ex.TokenName);
				throw;
			}
		}

		[TestMethod]
		public void ItShouldHandleIfElseConditionToken()
		{
			string part1 = "<%IF ";
			string part2 = "ConditionName";
			string part3 = " THEN%>";
			string part4 = "Content within IF";
			string part5 = "<%ELSE%>";
			string part6 = "More content";
			string part7 = "<%ENDIF%>";

			string templateText = part1 + part2 + part3 + part4 + part5 + part6 + part7;

			var parser = new Parser();
			List<IToken> result = parser.Parse(templateText);

			Assert.AreEqual(1, result.Count);

			var token = (ConditionToken) result[0];
			Assert.AreEqual(part2.ToLower(), token.Name);
			Assert.AreEqual(1, token.InnerTokens.Count);

			var content = (ContentToken) token.InnerTokens[0];
			Assert.AreEqual(part4, content.Content);
			Assert.AreEqual(1, token.FalsePart.InnerTokens.Count);

			content = (ContentToken) token.FalsePart.InnerTokens[0];
			Assert.AreEqual(part6, content.Content);
		}

		[TestMethod]
		public void ItShouldHandleIfElseIfConditionToken()
		{
			string part1 = "<%IF ";
			string part2 = "ConditionName";
			string part3 = " THEN%>";
			string part4 = "Content within IF";
			string part5 = "<%ELSEIF OtherCondition THEN%>";
			string part6 = "More content";
			string part7 = "<%ENDIF%>";

			string templateText = part1 + part2 + part3 + part4 + part5 + part6 + part7;

			var parser = new Parser();
			List<IToken> result = parser.Parse(templateText);

			Assert.AreEqual(1, result.Count);

			var token = (ConditionToken) result[0];
			Assert.AreEqual(part2.ToLower(), token.Name);
			Assert.AreEqual(1, token.InnerTokens.Count);

			var content = (ContentToken) token.InnerTokens[0];
			Assert.AreEqual(part4, content.Content);
			Assert.AreEqual(1, token.FalsePart.InnerTokens.Count);

			content = (ContentToken) token.FalsePart.InnerTokens[0];
			Assert.AreEqual(part6, content.Content);
		}

		[TestMethod]
		public void ItShouldHandleIfElseIfElseConditionToken()
		{
			string templateText = "<%IF ConditionName THEN%>";
			templateText += "Content within IF";
			templateText += "<%ELSEIF ElseIf1 THEN%>";
			templateText += "1st content";
			templateText += "<%ELSEIF ElseIf2 THEN%>";
			templateText += "2nd content";
			templateText += "<%ELSEIF ElseIf3%>";
			templateText += "3rd content";
			templateText += "<%ELSE%>";
			templateText += "else content <%=Something%>";
			templateText += "<%ENDIF%>";

			var parser = new Parser();
			List<IToken> result = parser.Parse(templateText);

			Assert.AreEqual(1, result.Count);

			var token = (ConditionToken) result[0];
			Assert.AreEqual("conditionname", token.Name);
			Assert.AreEqual(1, token.InnerTokens.Count);

			var content = (ContentToken) token.InnerTokens[0];
			Assert.AreEqual("Content within IF", content.Content);

			token = (ConditionToken) token.FalsePart;
			Assert.AreEqual("elseif1", token.Name);
			Assert.AreEqual(1, token.InnerTokens.Count);

			content = (ContentToken) token.InnerTokens[0];
			Assert.AreEqual("1st content", content.Content);

			token = (ConditionToken) token.FalsePart;
			Assert.AreEqual("elseif2", token.Name);
			Assert.AreEqual(1, token.InnerTokens.Count);

			content = (ContentToken) token.InnerTokens[0];
			Assert.AreEqual("2nd content", content.Content);

			token = (ConditionToken) token.FalsePart;
			Assert.AreEqual("elseif3", token.Name);
			Assert.AreEqual(1, token.InnerTokens.Count);

			content = (ContentToken) token.InnerTokens[0];
			Assert.AreEqual("3rd content", content.Content);

			var elseToken = (ElseToken) token.FalsePart;
			Assert.AreEqual(2, elseToken.InnerTokens.Count);

			content = (ContentToken) elseToken.InnerTokens[0];
			Assert.AreEqual("else content ", content.Content);

			Assert.AreEqual("something", ((NamedToken) elseToken.InnerTokens[1]).Name);
		}

		[TestMethod]
		[ExpectedException(typeof (TokenNotClosedException))]
		public void ItShouldThrowErrorElseIfWithoutEndIf()
		{
			string part1 = "<%IF ";
			string part2 = "ConditionName";
			string part3 = " THEN%>";
			string part4 = "Content within IF";
			string part5 = "<%ELSEIF OtherCondition THEN%>";
			string part6 = "More content";

			string templateText = part1 + part2 + part3 + part4 + part5 + part6;

			var parser = new Parser();
			try
			{
				parser.Parse(templateText);
			}
			catch (TokenNotClosedException ex)
			{
				Assert.AreEqual("othercondition", ex.TokenName);
				Assert.AreEqual("<%ELSEIF OtherCondition THEN%>", ex.Split);
				throw;
			}
		}

		[TestMethod]
		[ExpectedException(typeof (TokenNotClosedException))]
		public void ItShouldThrowErrorElseWithoutEndIf()
		{
			string part1 = "<%IF ";
			string part2 = "ConditionName";
			string part3 = " THEN%>";
			string part4 = "Content within IF";
			string part5 = "<%ELSE%>";
			string part6 = "More content";

			string templateText = part1 + part2 + part3 + part4 + part5 + part6;

			var parser = new Parser();
			try
			{
				parser.Parse(templateText);
			}
			catch (TokenNotClosedException ex)
			{
				Assert.AreEqual("conditionname", ex.TokenName);
				Assert.AreEqual("<%IF ConditionName THEN%>", ex.Split);
				throw;
			}
		}

		[TestMethod]
		public void ItShouldHandleBasicForEach()
		{
			string templateText = "<%FOREACH Name %>";
			templateText += "Content within for";
			templateText += "<%ENDFOR%>";

			var parser = new Parser();
			List<IToken> result = parser.Parse(templateText);

			Assert.AreEqual(1, result.Count);
			var token = (ForEachToken) result[0];
			Assert.AreEqual("name", token.Name);
			Assert.AreEqual(1, token.RowTokens.Count);

			var content = (ContentToken) token.RowTokens[0];
			Assert.AreEqual("Content within for", content.Content);
		}

		[TestMethod]
		[ExpectedException(typeof (TokenNotClosedException))]
		public void ItShouldThrowErrorWhenClosingEndForMissing1()
		{
			string templateText = "<%FOREACH Name %>";
			templateText += "Content within for";

			var parser = new Parser();
			try
			{
				parser.Parse(templateText);
			}
			catch (TokenNotClosedException ex)
			{
				Assert.AreEqual("name", ex.TokenName);
				Assert.AreEqual("<%FOREACH Name %>", ex.Split);
				throw;
			}
		}

		[TestMethod]
		[ExpectedException(typeof (TokenMissingNameException))]
		public void ItShouldThrowErrorWhenForEachNameMissing()
		{
			string templateText = "<%FOREACH %>";

			var parser = new Parser();
			try
			{
				parser.Parse(templateText);
			}
			catch (TokenMissingNameException ex)
			{
				Assert.AreEqual("<%FOREACH %>", ex.Split);
				throw;
			}
		}

		[TestMethod]
		public void ItShouldAbleToRespectRowTemplateElement()
		{
			string templateText = "<%FOREACH Name %>";
			templateText += "<%ROW%>Content within for<%ENDROW%>";
			templateText += "<%ENDFOR%>";

			var parser = new Parser();
			List<IToken> result = parser.Parse(templateText);

			Assert.AreEqual(1, result.Count);
			var token = (ForEachToken) result[0];
			Assert.AreEqual("name", token.Name);
			Assert.AreEqual(1, token.RowTokens.Count);

			var content = (ContentToken) token.RowTokens[0];
			Assert.AreEqual("Content within for", content.Content);
		}

		[TestMethod]
		[ExpectedException(typeof (TokenNotClosedException))]
		public void ItShouldThrowExceptionWhenRowNotClosed1()
		{
			string templateText = "<%FOREACH Name %>";
			templateText += "<%ROW%>Content within for";
			templateText += "<%ENDFOR%>";

			var parser = new Parser();
			try
			{
				parser.Parse(templateText);
			}
			catch (TokenNotClosedException ex)
			{
				Assert.AreEqual("name", ex.TokenName);
				Assert.AreEqual("<%FOREACH Name %>", ex.Split);
				Assert.AreEqual("<%ROW%> not closed for <%FOREACH Name %>", ex.Message);
				throw;
			}
		}

		[TestMethod]
		[ExpectedException(typeof (TokenNotClosedException))]
		public void ItShouldThrowExceptionWhenRowNotClosed2()
		{
			string templateText = "<%FOREACH Name %>";
			templateText += "<%ROW%>Content within for";

			var parser = new Parser();
			try
			{
				parser.Parse(templateText);
			}
			catch (TokenNotClosedException ex)
			{
				Assert.AreEqual("name", ex.TokenName);
				Assert.AreEqual("<%FOREACH Name %>", ex.Split);
				Assert.AreEqual("<%ROW%> not closed for <%FOREACH Name %>", ex.Message);
				throw;
			}
		}

		[TestMethod]
		[ExpectedException(typeof (TokenNotClosedException))]
		public void ItShouldThrowErrorWhenClosingEndForMissing2()
		{
			string templateText = "<%FOREACH Name %>";
			templateText += "<%ROW%>Content within for<%ENDROW%>";

			var parser = new Parser();
			try
			{
				parser.Parse(templateText);
			}
			catch (TokenNotClosedException ex)
			{
				Assert.AreEqual("name", ex.TokenName);
				Assert.AreEqual("<%FOREACH Name %>", ex.Split);
				throw;
			}
		}

		[TestMethod]
		public void ItShouldAbleToProcessVariousForInnerTokens()
		{
			string templateText = "<%FOREACH Name %>";

			templateText += "<%NORECORD%>Content within NORECORD<%ENDNORECORD%>";

			templateText += "<%HEADER%>Content within HEADER<%ENDHEADER%>";

			templateText += "<%BEFOREFIRSTROW%>Content within BEFOREFIRSTROW<%ENDBEFOREFIRSTROW%>";
			templateText += "<%FIRSTROW%>Content within FIRSTROW<%ENDFIRSTROW%>";
			templateText += "<%AFTERFIRSTROW%>Content within AFTERFIRSTROW<%ENDAFTERFIRSTROW%>";

			templateText += "<%BEFOREROW%>Content within BEFOREROW<%ENDBEFOREROW%>";
			templateText += "<%ROW%>Content within ROW<%ENDROW%>";
			templateText += "<%AFTERROW%>Content within AFTERROW<%ENDAFTERROW%>";

			templateText += "<%BEFOREALTROW%>Content within BEFOREALTROW<%ENDBEFOREALTROW%>";
			templateText += "<%ALTROW%>Content within ALTROW<%ENDALTROW%>";
			templateText += "<%AFTERALTROW%>Content within AFTERALTROW<%ENDAFTERALTROW%>";

			templateText += "<%BEFORELASTROW%>Content within BEFORELASTROW<%ENDBEFORELASTROW%>";
			templateText += "<%LASTROW%>Content within LASTROW<%ENDLASTROW%>";
			templateText += "<%AFTERLASTROW%>Content within AFTERLASTROW<%ENDAFTERLASTROW%>";

			templateText += "<%FOOTER%>Content within FOOTER<%ENDFOOTER%>";

			templateText += "<%ENDFOR%>";

			var parser = new Parser();
			List<IToken> result = parser.Parse(templateText);

			var assertMethod = new Action<List<IToken>, string>(
				(tokenList,  innerContent) =>
				{
					Assert.AreEqual(1, tokenList.Count);
					var content = (ContentToken) tokenList[0];
					Assert.AreEqual(innerContent, content.Content);
				});

			Assert.AreEqual(1, result.Count);
			var token = (ForEachToken) result[0];
			Assert.AreEqual("name", token.Name);

			assertMethod(token.NoRecordTokens, "Content within NORECORD");
			assertMethod(token.HeaderTokens, "Content within HEADER");

			assertMethod(token.BeforeFirstRowTokens, "Content within BEFOREFIRSTROW");
			assertMethod(token.FirstRowTokens, "Content within FIRSTROW");
			assertMethod(token.AfterFirstRowTokens, "Content within AFTERFIRSTROW");

			assertMethod(token.BeforeRowTokens, "Content within BEFOREROW");
			assertMethod(token.RowTokens, "Content within ROW");
			assertMethod(token.AfterRowTokens, "Content within AFTERROW");

			assertMethod(token.BeforeAltRowTokens, "Content within BEFOREALTROW");
			assertMethod(token.AltRowTokens, "Content within ALTROW");
			assertMethod(token.AfterAltRowTokens, "Content within AFTERALTROW");

			assertMethod(token.BeforeLastRowTokens, "Content within BEFORELASTROW");
			assertMethod(token.LastRowTokens, "Content within LASTROW");
			assertMethod(token.AfterLastRowTokens, "Content within AFTERLASTROW");

			assertMethod(token.FooterTokens, "Content within FOOTER");
		}

		[TestMethod]
		public void ItShouldThrowErrorForNotClosingVariousForInnerTokens()
		{
			const string templateBegin = "<%FOREACH Name %>";
			const string templateEnd = "<%ENDFOR%>";

			var assertMethod = new Action<string>(
				(string token) =>
				{
					string templateText = templateBegin + "<%" + token + "%>Content within " + token + templateEnd;
					var parser = new Parser();
					try
					{
						parser.Parse(templateText);
					}
					catch (TokenNotClosedException ex)
					{
						Assert.AreEqual("name", ex.TokenName);
						Assert.AreEqual("<%FOREACH Name %>", ex.Split);
						Assert.AreEqual("<%" + token + "%> not closed for <%FOREACH Name %>", ex.Message);
						return;
					}

					Assert.IsTrue(false); //this is to ensure try catch actually ran.
				});

			assertMethod("NORECORD");
			assertMethod("HEADER");

			assertMethod("BEFOREFIRSTROW");
			assertMethod("FIRSTROW");
			assertMethod("AFTERFIRSTROW");

			assertMethod("BEFOREROW");
			assertMethod("ROW");
			assertMethod("AFTERROW");

			assertMethod("BEFOREALTROW");
			assertMethod("ALTROW");
			assertMethod("AFTERALTROW");

			assertMethod("BEFORELASTROW");
			assertMethod("LASTROW");
			assertMethod("AFTERLASTROW");

			assertMethod("FOOTER");
		}

		[TestMethod]
		[ExpectedException(typeof (ParserException))]
		public void ItShouldThrowExceptionWithInvalidToken1()
		{
			string templateText = "<%FOREACH Name %>";

			templateText += "<%NORECORD%>Content within NORECORD<%ENDNORECORD%>";

			templateText += "<%HEADER%>Content within HEADER<%ENDHEADER%>";

			templateText += "<%BEFOREFIRSTROW%>Content within BEFOREFIRSTROW<%ENDBEFOREFIRSTROW%>";
			templateText += "<%FIRSTROW%>Content within FIRSTROW<%ENDFIRSTROW%>";
			templateText += "<%AFTERFIRSTROW%>Content within AFTERFIRSTROW<%ENDAFTERFIRSTROW%>";

			templateText += "<%BEFOREROW%>Content within BEFOREROW<%ENDBEFOREROW%>";
			templateText += "<%ROW%>Content within ROW<%ENDROW%>";
			templateText += "<%AFTERROW%>Content within AFTERROW<%ENDAFTERROW%>";

			templateText += "<%BEFOREALTROW%>Content within BEFOREALTROW<%ENDBEFOREALTROW%>";
			templateText += "<%ALTROW%>Content within ALTROW<%ENDALTROW%>";
			templateText += "<%AFTERALTROW%>Content within AFTERALTROW<%ENDAFTERALTROW%>";

			templateText += "<%BEFORELASTROW%>Content within BEFORELASTROW<%ENDBEFORELASTROW%>";
			templateText += "<%LASTROW%>Content within LASTROW<%ENDLASTROW%>";
			templateText += "<%AFTERLASTROW%>Content within AFTERLASTROW<%ENDAFTERLASTROW%>";

			templateText += "<%FOOTER%>Content within FOOTER<%ENDFOOTER%>";

			templateText += "<%FOOTER %>Content within FOOTER<%ENDFOOTER%>";

			templateText += "<%ENDFOR%>";

			var parser = new Parser();
			parser.Parse(templateText);
		}

		[TestMethod]
		[ExpectedException(typeof (ParserException))]
		public void ItShouldThrowExceptionWithInvalidToken2()
		{
			string templateText = "<%FOREACH Name %>";

			templateText += "<%NORECORD %>Content within NORECORD<%ENDNORECORD%>";

			templateText += "<%FOOTER%>Content within FOOTER<%ENDFOOTER%>";

			templateText += "<%ENDFOR%>";

			var parser = new Parser();
			parser.Parse(templateText);
		}

		[TestMethod]
		[ExpectedException(typeof (ParserException))]
		public void ItShouldThrowExceptionWithInvalidToken3()
		{
			string templateText = "<%FOREACH Name %>";

			templateText += "<%NORECORD%>Content within NORECORD<%ENDNORECORD%>";
			templateText += "<%NORECORD%>Content within NORECORD<%ENDNORECORD%>"; //duplicate token will throw error.
			templateText += "<%ENDFOR%>";

			var parser = new Parser();
			parser.Parse(templateText);
		}

		[TestMethod]
		public void ItShouldHandleBasicWithStatement()
		{
			const string templateText = "<%WITH Name %>Some content <%=SomeToken%><%ENDWITH%>";
			var parser = new Parser();
			List<IToken> result = parser.Parse(templateText);

			Assert.AreEqual(1, result.Count);
			var token = (WithToken) result[0];
			Assert.AreEqual("name", token.Name);

			Assert.AreEqual(2, token.InnerTokens.Count);
			var content = (ContentToken) token.InnerTokens[0];
			Assert.AreEqual("Some content ", content.Content);

			var namedContent = (NamedToken) token.InnerTokens[1];
			Assert.AreEqual("sometoken", namedContent.Name);
		}

		[TestMethod]
		[ExpectedException(typeof (TokenMissingNameException))]
		public void ItShouldThrowExceptionForMissingNameInWith()
		{
			const string templateText = "<%WITH %>Some content<%ENDWITH%>";
			var parser = new Parser();
			parser.Parse(templateText);
		}

		[TestMethod]
		[ExpectedException(typeof (TokenNotClosedException))]
		public void ItShouldThrowExceptionForMissingEndForWith()
		{
			const string templateText = "<%WITH Name %>Some content";
			var parser = new Parser();
			parser.Parse(templateText);
		}

		[TestMethod]
		public void ItShouldHandleBasicReuseForEachToken()
		{
			const string templateText =
				"<%FOREACH ExistingForEachName %><%ENDFOR%><%REUSEFOREACH ExistingForEachName NewLoopName %>";
			var parser = new Parser();
			List<IToken> result = parser.Parse(templateText);

			Assert.AreEqual(2, result.Count);
			var token = (ReuseForEachToken) result[1];
			Assert.AreEqual("newloopname", token.Name);
			Assert.AreEqual("existingforeachname", token.ExistingForEachName);
		}

		[TestMethod]
		public void ItShouldHandleBasicReuseWithInForEachToken()
		{
			const string templateText =
				"<%FOREACH ExistingForEachName %><%REUSEFOREACH ExistingForEachName NewLoopName %><%ENDFOR%>";
			var parser = new Parser();
			List<IToken> result = parser.Parse(templateText);

			Assert.AreEqual(1, result.Count);
			var forToken = (ForEachToken)result[0];
			var token = (ReuseForEachToken) forToken.RowTokens[0];
			Assert.AreEqual("newloopname", token.Name);
			Assert.AreEqual("existingforeachname", token.ExistingForEachName);
		}

		[TestMethod]
		[ExpectedException(typeof (TokenMissingNameException))]
		public void ItShouldThrowExceptionForMissingNameForReuseForEach()
		{
			const string templateText = "<%REUSEFOREACH ExistingForEachName %>";
			var parser = new Parser();
			parser.Parse(templateText);
		}

		[TestMethod]
		[ExpectedException(typeof (TokenMissingNameException))]
		public void ItShouldThrowExceptionForMissingLoopNameForReuseForEach()
		{
			const string templateText = "<%REUSEFOREACH %>";
			var parser = new Parser();
			parser.Parse(templateText);
		}

		[TestMethod]
		[ExpectedException(typeof (ForEachMissingForReuseException))]
		public void ItShouldThrowExceptionIfGivenForEachNameDoesntPreeceedReuse()
		{
			const string templateText = "<%REUSEFOREACH ExistingForEachName NewLoopName %>";
			var parser = new Parser();
			parser.Parse(templateText);
		}

		[TestMethod]
		[ExpectedException(typeof(ForEachMissingForReuseException))]
		public void ItShouldThrowExceptionIfGivenForEachNameDoesntPreeceedReuse2()
		{
			const string templateText = "<%FOREACH SomeName%><%ENDFOR%><%REUSEFOREACH ExistingForEachName NewLoopName %>";
			var parser = new Parser();
			parser.Parse(templateText);
		}

		[TestMethod]
		public void ItShouldAbleToTakeBasicAttribute()
		{
			const string templateText = "<%=Token name=\"Value\" name2=\"value2\"%>";
			var parser = new Parser();
			List<IToken> result = parser.Parse(templateText);

			Assert.AreEqual(1, result.Count);
			var token = (NamedToken) result[0];
			Assert.AreEqual("token", token.Name);
			Assert.AreEqual(2, token.Attributes.Count);
			Assert.AreEqual("Value", token.Attributes["name"]);
			Assert.AreEqual("value2", token.Attributes["name2"]);
		}

		[TestMethod]
		[ExpectedException(typeof (InvalidTokenAttributeException))]
		public void ItShouldThrowExceptionIfAttributeNotCorrect1()
		{
			const string templateText = "<%=Token name=Value%>";
			var parser = new Parser();
			parser.Parse(templateText);
		}

		[TestMethod]
		[ExpectedException(typeof (InvalidTokenAttributeException))]
		public void ItShouldThrowExceptionIfAttributeNotCorrect2()
		{
			const string templateText = "<%=Token name=\"Value%>";
			var parser = new Parser();
			parser.Parse(templateText);
		}

		[TestMethod]
		[ExpectedException(typeof (InvalidTokenAttributeException))]
		public void ItShouldThrowExceptionIfAttributeNotCorrect3()
		{
			const string templateText = "<%=Token name=Value\"%>";
			var parser = new Parser();
			parser.Parse(templateText);
		}

		[TestMethod]
		[ExpectedException(typeof (InvalidTokenAttributeException))]
		public void ItShouldThrowExceptionIfAttributeNotCorrect4()
		{
			const string templateText = "<%=Token name=%>";
			var parser = new Parser();
			parser.Parse(templateText);
		}

		[TestMethod]
		[ExpectedException(typeof (InvalidTokenAttributeException))]
		public void ItShouldThrowExceptionIfAttributeNotCorrect5()
		{
			const string templateText = "<%=Token =\"value\"%>";
			var parser = new Parser();
			parser.Parse(templateText);
		}

		[TestMethod]
		[ExpectedException(typeof (InvalidTokenAttributeException))]
		public void ItShouldThrowExceptionIfAttributeNotCorrect6()
		{
			const string templateText = "<%=Token name = \"value\"%>";
			var parser = new Parser();
			parser.Parse(templateText);
		}

		[TestMethod]
		public void ItShouldAbleToTakeBasicAttributeForWithToken()
		{
			const string templateText = "<%WITH WithName name=\"Value\" name2=\"value2\"%> some content <%ENDWITH%>";
			var parser = new Parser();
			List<IToken> result = parser.Parse(templateText);

			Assert.AreEqual(1, result.Count);
			var token = (WithToken) result[0];
			Assert.AreEqual("withname", token.Name);
			Assert.AreEqual(2, token.Attributes.Count);
			Assert.AreEqual("Value", token.Attributes["name"]);
			Assert.AreEqual("value2", token.Attributes["name2"]);
		}
	}
}