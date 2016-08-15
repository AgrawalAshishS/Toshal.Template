using System.Collections.Generic;
using System.Text;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.Template.SupportClass;

namespace Test.Template
{
    using Toshal.Template;
    using Toshal.Template.Tokens;

    [TestClass]
	public class TemplateProcessorTests
	{
		[TestMethod]
		public void BasicTextContentOnlyTemplate()
		{
			string templateText = "basic text";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);
			var processor = new Processor();
			StringBuilder result = processor.Process(new ProcessorArgs(tokens));
			Assert.AreEqual(templateText, result.ToString());
		}

		[TestMethod]
		public void BasicNamedTokenTemplate()
		{
			string templateText = "<%=Token%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);
			var processor = new Processor();
			StringBuilder result = processor.Process(new ProcessorArgs(tokens));
			Assert.AreEqual("", result.ToString());
		}

		[TestMethod]
		public void BasicNamedTokenTemplateWithValueReplacement()
		{
			string templateText = "<%=Token%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.TokenValueProvider = (TokenArgs arg) => "some";

			StringBuilder result = processor.Process(new ProcessorArgs(tokens));
			Assert.AreEqual("some", result.ToString());
		}

		[TestMethod]
		public void BasicNamedTokenTemplateWithAttributes()
		{
			string templateText = "<%=Token attr=\"Name\"%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.TokenValueProvider = (TokenArgs args) => args.Attributes["attr"];

			StringBuilder result = processor.Process(new ProcessorArgs(tokens));
			Assert.AreEqual("Name", result.ToString());
		}

		[TestMethod]
		public void BasicNamedTokenTemplateWithMultipleTokens()
		{
			string templateText = "<%=Token1%>, <%=Token2%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.TokenValueProvider = (TokenArgs args) =>
			{
				switch (args.Name)
				{
					case "token1":
						return "First Value";
					case "token2":
						return "Second Value";
				}
				return null;
			};

			StringBuilder result = processor.Process(new ProcessorArgs(tokens));
			Assert.AreEqual("First Value, Second Value", result.ToString());
		}

		/*
		 * If no provider
		 * if with false
		 * if with true
		 * if else with true
		 * if else with false
		 * if else if with true
		 * if else if with false true
		 * if else if with false false
		 * if else if else with false true
		 * if else if else with false false
		 */

		[TestMethod]
		public void BasicConditionToken()
		{
			string templateText = "<%IF ConditionName THEN%>Inner Content<%ENDIF%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("", result.ToString());
		}

		[TestMethod]
		public void BasicConditionTokenWithProviderAndFalseValue()
		{
			string templateText = "<%IF ConditionName THEN%>Inner Content<%ENDIF%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.ConditionValueProvider = (ConditionArgs args) => false;
			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("", result.ToString());
		}

		[TestMethod]
		public void BasicConditionTokenWithProviderAndTrueValue()
		{
			string templateText = "<%IF ConditionName THEN%>Inner Content<%ENDIF%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.ConditionValueProvider = (ConditionArgs args) => true;
			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("Inner Content", result.ToString());
		}

		[TestMethod]
		public void BasicConditionTokenWithProviderReturnBasedOnContext()
		{
			string templateText = "<%IF ConditionName THEN%>Inner Content<%ENDIF%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);
			var user = new TestUser {Name = "My User"};

			var processor = new Processor();
			processor.ConditionValueProvider =
				(ConditionArgs args) => { return ((TestUser) args.Context).Name == "ConditionName"; };

			var process = new ProcessorArgs(tokens);
			process.Context = user;

			StringBuilder result = processor.Process(process);
			Assert.AreEqual("", result.ToString());

			user = new TestUser {Name = "ConditionName"};
			process = new ProcessorArgs(tokens);
			process.Context = user;

			result = processor.Process(process);
			Assert.AreEqual("Inner Content", result.ToString());
		}

		[TestMethod]
		public void BasicIfElseConditionTokenAndTrueValue()
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
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.ConditionValueProvider = (ConditionArgs args) => true;

			var process = new ProcessorArgs(tokens);

			StringBuilder result = processor.Process(process);

			Assert.AreEqual("Content within IF", result.ToString());
		}

		[TestMethod]
		public void BasicIfElseConditionTokenAndFalseValue()
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
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.ConditionValueProvider = (ConditionArgs args) => false;

			var process = new ProcessorArgs(tokens);

			StringBuilder result = processor.Process(process);

			Assert.AreEqual("More content", result.ToString());
		}

		[TestMethod]
		public void BasicIfElseIfConditionTokenAndFirstConditionTrueValue()
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
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.ConditionValueProvider = (ConditionArgs args) => true;

			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);

			Assert.AreEqual("Content within IF", result.ToString());
		}

		[TestMethod]
		public void BasicIfElseIfConditionTokenAndFirstIsFalseSecondIsTrueValue()
		{
			string part1 = "<%IF ";
			string part2 = "ConditionName";
			string part3 = " THEN%>";
			string part4 = "Content within IF";
			string part5 = "<%ELSEIF OtherCondition THEN%>";
			string part6 = "More content";
			string part9 = "<%ENDIF%>";

			string templateText = part1 + part2 + part3 + part4 + part5 + part6 + part9;

			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.ConditionValueProvider = (ConditionArgs args) =>
			{
				switch (args.Name)
				{
					case "conditionname":
						return false;
					case "othercondition":
						return true;
				}
				return false;
			};

			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);

			Assert.AreEqual("More content", result.ToString());
		}

		[TestMethod]
		public void BasicIfElseIfElseConditionTokenForLastElseCheck()
		{
			string part1 = "<%IF ";
			string part2 = "ConditionName";
			string part3 = " THEN%>";
			string part4 = "Content within IF";
			string part5 = "<%ELSEIF OtherCondition THEN%>";
			string part6 = "More content";
			string part9 = "<%ELSE%>";
			string part10 = "Else Content";
			string part11 = "<%ENDIF%>";

			string templateText = part1 + part2 + part3 + part4 + part5 + part6 + part9 + part10 + part11;

			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.ConditionValueProvider = (ConditionArgs args) => false;

			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);

			Assert.AreEqual("Else Content", result.ToString());
		}

		[TestMethod]
		public void BasicIfElseIfConditionTokenWithAllFalse()
		{
			string part1 = "<%IF ";
			string part2 = "ConditionName";
			string part3 = " THEN%>";
			string part4 = "Content within IF";
			string part5 = "<%ELSEIF OtherCondition THEN%>";
			string part6 = "More content";
			string part7 = "<%ELSEIF OtherCondition THEN%>";
			string part8 = "More content";
			string part9 = "<%ENDIF%>";

			string templateText = part1 + part2 + part3 + part4 + part5 + part6 + part7 + part8 + part9;

			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.ConditionValueProvider = (ConditionArgs args) => false;
			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);

			Assert.AreEqual("", result.ToString());
		}

		/*
		 * for without value
		 * for with single value
		 * for with 3 values
		 * for with norecord template - no value
		 * for with norecord template - one value
		 * for header & footer - no value
		 * for header & footer & No record - no value
		 * for header & footer - one value
		 * for header & footer - multiple value
		 * 
		 * for row with 3 values
		 * for row with before row template 3 values
		 * for row with after row template 3 values
		 * for row with before & after row template 3 values
		 * 
		 * for row and alternate row with 3 values
		 * for before after templates for row and alternate 3 values
		 * 
		 * for row and only before-after alternate (no alternate row) with 3 values --- row fallback
		 * for row & alternate row - only before-after row with 3 values -- before-after fallback
		 * 
		 * for first and last row and row template - with 3 values
		 * for first and last row and alternate row template - with 3 values -- alternate row should be applied
		 * for first and last row template - with 3 values (no row content)
		 * 
		 * for before-after first row but no first row and row template -- row fallback
		 * for first row and before-after + row template -- before-after fallback
		 * 
		 * for before-after last row but no last row and row template -- row fallback
		 * for last row and before-after + row template -- before-after fallback
		 * 
		 * for before-after last row but no last row and alternate row + row template with 2 values -- alternate row fallback
		 * for last row and before-after + alternate row + row template with 2 elements -- before-after fallback
		 */

		[TestMethod]
		public void BasicForEachToken()
		{
			string templateText = "<%FOREACH LoopName%>Inner Content<%ENDFOR%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("", result.ToString());
		}

		[TestMethod]
		public void BasicForEachTokenWithProviderAndNoValue()
		{
			string templateText = "<%FOREACH LoopName%>Inner Content<%ENDFOR%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => null;
			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("", result.ToString());
		}

		[TestMethod]
		public void BasicForEachTokenWithProviderAndOneValue()
		{
			string templateText = "<%FOREACH LoopName%><%=Value%><%ENDFOR%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => new List<int> {1};
			processor.TokenValueProvider = (TokenArgs args) => args.Context.ToString();

			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("1", result.ToString());
		}

		[TestMethod]
		public void BasicForEachTokenWithProviderAndMultipleValues()
		{
			string templateText = "<%FOREACH LoopName%><%=Value%><%ENDFOR%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => new List<int> {1, 2, 3};
			processor.TokenValueProvider = (TokenArgs args) => args.Context.ToString();

			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("123", result.ToString());
		}

		[TestMethod]
		public void ForEachWithNoRecordAndNoValue()
		{
			string templateText = "<%FOREACH LoopName%><%NORECORD%>Some<%ENDNORECORD%><%ENDFOR%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => null;
			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("Some", result.ToString());
		}

		[TestMethod]
		public void ForEachWithNoRecordAndOneValue()
		{
			string templateText = "<%FOREACH LoopName%><%NORECORD%>Some<%ENDNORECORD%><%ROW%>row content<%ENDROW%><%ENDFOR%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => new List<int> {1};
			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("row content", result.ToString());
		}

		[TestMethod]
		public void ForEachWithNoRecordHeaderFooterAndNoValue()
		{
			string templateText =
				"<%FOREACH LoopName%><%NORECORD%>Nothing<%ENDNORECORD%><%HEADER%>Head<%ENDHEADER%><%FOOTER%>Foot<%ENDFOOTER%><%ENDFOR%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => null;
			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("Nothing", result.ToString());
		}

		[TestMethod]
		public void ForEachWithHeaderFooterAndNoValue()
		{
			string templateText = "<%FOREACH LoopName%><%HEADER%>Head<%ENDHEADER%><%FOOTER%>Foot<%ENDFOOTER%><%ENDFOR%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => null;
			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("", result.ToString());
		}

		[TestMethod]
		public void ForEachWithHeaderFooterAndOneValue()
		{
			string templateText = "<%FOREACH LoopName%><%HEADER%>Head<%ENDHEADER%><%FOOTER%>Foot<%ENDFOOTER%><%ENDFOR%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => new List<int> {1};
			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("HeadFoot", result.ToString());
		}

		[TestMethod]
		public void ForEachWithHeaderFooterAndMultipleValue()
		{
			string templateText = "<%FOREACH LoopName%><%HEADER%>Head<%ENDHEADER%><%FOOTER%>Foot<%ENDFOOTER%><%ENDFOR%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => new List<int> {1, 2, 3};
			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("HeadFoot", result.ToString());
		}

		[TestMethod]
		public void ForEachWithRowAndMultipleValues()
		{
			string templateText = "<%FOREACH LoopName%><%ROW%><%=Value%><%ENDROW%><%ENDFOR%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => new List<int> {1, 2, 3};
			processor.TokenValueProvider = (TokenArgs args) => args.Context.ToString();

			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("123", result.ToString());
		}

		[TestMethod]
		public void ForEachWithRowBeforeAndMultipleValues()
		{
			string templateText = "<%FOREACH LoopName%><%BEFOREROW%>A<%ENDBEFOREROW%><%ROW%><%=Value%><%ENDROW%><%ENDFOR%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => new List<int> {1, 2, 3};
			processor.TokenValueProvider = (TokenArgs args) => args.Context.ToString();

			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("A1A2A3", result.ToString());
		}

		[TestMethod]
		public void ForEachWithRowAfterAndMultipleValues()
		{
			string templateText = "<%FOREACH LoopName%><%ROW%><%=Value%><%ENDROW%><%AFTERROW%>B<%ENDAFTERROW%><%ENDFOR%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => new List<int> {1, 2, 3};
			processor.TokenValueProvider = (TokenArgs args) => args.Context.ToString();

			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("1B2B3B", result.ToString());
		}

		[TestMethod]
		public void ForEachWithRowBeforeAfterAndMultipleValues()
		{
			string templateText =
				"<%FOREACH LoopName%><%BEFOREROW%>A<%ENDBEFOREROW%><%ROW%><%=Value%><%ENDROW%><%AFTERROW%>B<%ENDAFTERROW%><%ENDFOR%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => new List<int> {1, 2, 3};
			processor.TokenValueProvider = (TokenArgs args) => args.Context.ToString();

			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("A1BA2BA3B", result.ToString());
		}

		[TestMethod]
		public void ForEachWithRowAlternateAndMultipleValues()
		{
			string templateText = "<%FOREACH LoopName%><%ROW%><%=Value%>A<%ENDROW%><%ALTROW%><%=Value%>B<%ENDALTROW%><%ENDFOR%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => new List<int> {1, 2, 3};
			processor.TokenValueProvider = (TokenArgs args) => args.Context.ToString();

			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("1A2B3A", result.ToString());
		}

		[TestMethod]
		public void ForEachWithRowAlternateBeforeAndAfterForBothAndMultipleValues()
		{
			string templateText = "<%FOREACH LoopName%>";
			templateText += "<%BEFOREROW%>-<%ENDBEFOREROW%>";
			templateText += "<%ROW%><%=Value%>A<%ENDROW%>";
			templateText += "<%AFTERROW%>=<%ENDAFTERROW%>";
			templateText += "<%BEFOREALTROW%>+<%ENDBEFOREALTROW%>";
			templateText += "<%ALTROW%><%=Value%>B<%ENDALTROW%>";
			templateText += "<%AFTERALTROW%>!<%ENDAFTERALTROW%>";
			templateText += "<%ENDFOR%>";

			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => new List<int> {1, 2, 3};
			processor.TokenValueProvider = (TokenArgs args) => args.Context.ToString();

			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("-1A=+2B!-3A=", result.ToString());
		}

		[TestMethod]
		public void ForEachWithRowAlternateBeforeAndAfterForRowOnlyAndMultipleValues()
		{
			string templateText = "<%FOREACH LoopName%>";
			templateText += "<%BEFOREROW%>-<%ENDBEFOREROW%>";
			templateText += "<%ROW%><%=Value%>A<%ENDROW%>";
			templateText += "<%AFTERROW%>=<%ENDAFTERROW%>";
			templateText += "<%ALTROW%><%=Value%>B<%ENDALTROW%>";
			templateText += "<%ENDFOR%>";

			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => new List<int> {1, 2, 3};
			processor.TokenValueProvider = (TokenArgs args) => args.Context.ToString();

			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("-1A=-2B=-3A=", result.ToString());
		}

		[TestMethod]
		public void ForEachWithRowNoAlternateAndBeforeAndAfterForBothAndMultipleValues()
		{
			string templateText = "<%FOREACH LoopName%>";
			templateText += "<%BEFOREROW%>-<%ENDBEFOREROW%>";
			templateText += "<%ROW%><%=Value%>A<%ENDROW%>";
			templateText += "<%AFTERROW%>=<%ENDAFTERROW%>";
			templateText += "<%BEFOREALTROW%>+<%ENDBEFOREALTROW%>";
			templateText += "<%AFTERALTROW%>!<%ENDAFTERALTROW%>";
			templateText += "<%ENDFOR%>";

			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => new List<int> {1, 2, 3};
			processor.TokenValueProvider = (TokenArgs args) => args.Context.ToString();

			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("-1A=+2A!-3A=", result.ToString());
		}

		[TestMethod]
		public void ForEachWithFirstLastAndMultipleValues()
		{
			string templateText = "<%FOREACH LoopName%>";
			templateText += "<%FIRSTROW%>-<%=Value%><%ENDFIRSTROW%>";
			templateText += "<%LASTROW%>=<%=Value%><%ENDLASTROW%>";
			templateText += "<%ENDFOR%>";

			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => new List<int> {1, 2, 3};
			processor.TokenValueProvider = (TokenArgs args) => args.Context.ToString();

			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("-1=3", result.ToString());
		}

		[TestMethod]
		public void ForEachWithFirstLastAndRowAndMultipleValues()
		{
			string templateText = "<%FOREACH LoopName%>";
			templateText += "<%FIRSTROW%>-<%=Value%><%ENDFIRSTROW%>";
			templateText += "<%ROW%>+<%=Value%><%ENDROW%>";
			templateText += "<%LASTROW%>=<%=Value%><%ENDLASTROW%>";
			templateText += "<%ENDFOR%>";

			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => new List<int> {1, 2, 3};
			processor.TokenValueProvider = (TokenArgs args) => args.Context.ToString();

			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("-1+2=3", result.ToString());
		}

		[TestMethod]
		public void ForEachWithFirstLastAndAltRowAndMultipleValues()
		{
			string templateText = "<%FOREACH LoopName%>";
			templateText += "<%FIRSTROW%>-<%=Value%><%ENDFIRSTROW%>";
			templateText += "<%ALTROW%>+<%=Value%>A<%ENDALTROW%>";
			templateText += "<%LASTROW%>=<%=Value%><%ENDLASTROW%>";
			templateText += "<%ENDFOR%>";

			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => new List<int> {1, 2, 3};
			processor.TokenValueProvider = (TokenArgs args) => args.Context.ToString();

			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("-1+2A=3", result.ToString());
		}

		[TestMethod]
		public void ForEachWithBeforeAfterFirstAndRowAndMultipleValues()
		{
			string templateText = "<%FOREACH LoopName%>";
			templateText += "<%BEFOREFIRSTROW%>-<%ENDBEFOREFIRSTROW%>";
			templateText += "<%AFTERFIRSTROW%>=<%ENDAFTERFIRSTROW%>";
			templateText += "<%ROW%><%=Value%><%ENDROW%>";
			templateText += "<%ENDFOR%>";

			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => new List<int> {1, 2, 3};
			processor.TokenValueProvider = (TokenArgs args) => args.Context.ToString();

			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("-1=23", result.ToString());
		}

		[TestMethod]
		public void ForEachWithBeforeAfterRowFirstAndRowAndMultipleValues()
		{
			string templateText = "<%FOREACH LoopName%>";
			templateText += "<%FIRSTROW%><%=Value%>F<%ENDFIRSTROW%>";
			templateText += "<%BEFOREROW%>-<%ENDBEFOREROW%>";
			templateText += "<%ROW%><%=Value%><%ENDROW%>";
			templateText += "<%AFTERROW%>!<%ENDAFTERROW%>";
			templateText += "<%ENDFOR%>";

			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => new List<int> {1, 2, 3};
			processor.TokenValueProvider = (TokenArgs args) => args.Context.ToString();

			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("-1F!-2!-3!", result.ToString());
		}

		[TestMethod]
		public void ForEachWithBeforeAfterLastAndRowAndMultipleValues()
		{
			string templateText = "<%FOREACH LoopName%>";
			templateText += "<%BEFORELASTROW%>-<%ENDBEFORELASTROW%>";
			templateText += "<%AFTERLASTROW%>=<%ENDAFTERLASTROW%>";
			templateText += "<%ROW%><%=Value%><%ENDROW%>";
			templateText += "<%ENDFOR%>";

			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => new List<int> {1, 2, 3};
			processor.TokenValueProvider = (TokenArgs args) => args.Context.ToString();

			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("12-3=", result.ToString());
		}

		[TestMethod]
		public void ForEachWithBeforeAfterRowLastAndRowAndMultipleValues()
		{
			string templateText = "<%FOREACH LoopName%>";
			templateText += "<%LASTROW%><%=Value%>L<%ENDLASTROW%>";
			templateText += "<%BEFOREROW%>-<%ENDBEFOREROW%>";
			templateText += "<%ROW%><%=Value%><%ENDROW%>";
			templateText += "<%AFTERROW%>!<%ENDAFTERROW%>";
			templateText += "<%ENDFOR%>";

			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => new List<int> {1, 2, 3};
			processor.TokenValueProvider = (TokenArgs args) => args.Context.ToString();

			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("-1!-2!-3L!", result.ToString());
		}

		[TestMethod]
		public void ForEachWithBeforeAfterLastAndAltRowAndMultipleValues()
		{
			string templateText = "<%FOREACH LoopName%>";
			templateText += "<%BEFORELASTROW%>-<%ENDBEFORELASTROW%>";
			templateText += "<%AFTERLASTROW%>=<%ENDAFTERLASTROW%>";
			templateText += "<%ALTROW%><%=Value%><%ENDALTROW%>";
			templateText += "<%ENDFOR%>";

			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => new List<int> {1, 2};
			processor.TokenValueProvider = (TokenArgs args) => args.Context.ToString();

			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("-2=", result.ToString());
		}

		[TestMethod]
		public void ForEachWithBeforeAfterRowLastAndAltRowAndMultipleValues()
		{
			string templateText = "<%FOREACH LoopName%>";
			templateText += "<%LASTROW%><%=Value%>L<%ENDLASTROW%>";
			templateText += "<%BEFOREALTROW%>-<%ENDBEFOREALTROW%>";
			templateText += "<%ROW%><%=Value%><%ENDROW%>";
			templateText += "<%AFTERALTROW%>!<%ENDAFTERALTROW%>";
			templateText += "<%ENDFOR%>";

			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => new List<int> {1, 2};
			processor.TokenValueProvider = (TokenArgs args) => args.Context.ToString();

			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("1-2L!", result.ToString());
		}

		[TestMethod]
		public void ForEachWithBeforeAfterFirstLastAndRowAndMultipleValues()
		{
			string templateText = "<%FOREACH LoopName%>";
			templateText += "<%BEFOREFIRSTROW%>-<%ENDBEFOREFIRSTROW%>";
			templateText += "<%AFTERFIRSTROW%>=<%ENDAFTERFIRSTROW%>";
			templateText += "<%ROW%><%=Value%><%ENDROW%>";
			templateText += "<%BEFORELASTROW%>+<%ENDBEFORELASTROW%>";
			templateText += "<%AFTERLASTROW%>!<%ENDAFTERLASTROW%>";
			templateText += "<%ENDFOR%>";

			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => new List<int> {1, 2, 3};
			processor.TokenValueProvider = (TokenArgs args) => args.Context.ToString();

			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("-1=2+3!", result.ToString());
		}

		/*context based tests*/

		[TestMethod]
		public void BasicNamedTokenTemplateWithValuePickedFromContext()
		{
			string templateText = "<%=Name%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);
			var user = new TestUser {Name = "My User"};

			var processor = new Processor();
			processor.TokenValueProvider = (TokenArgs args) =>
			{
				switch (args.Name)
				{
					case "name":
						return ((TestUser) args.Context).Name;
				}
				return null;
			};

			var process = new ProcessorArgs(tokens);
			process.Context = user;
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("My User", result.ToString());
		}

		[TestMethod]
		public void BasicIfElseIfConditionTokenWithProviderAndFalseValueReturnBasedOnContext()
		{
			string part1 = "<%IF ";
			string part2 = "IsTestUser";
			string part3 = " THEN%>";
			string part4 = "Content within IF";
			string part5 = "<%ELSEIF OtherCondition THEN%>";
			string part6 = "More content";
			string part7 = "<%ENDIF%>";

			string templateText = part1 + part2 + part3 + part4 + part5 + part6 + part7;

			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.ConditionValueProvider =
				(ConditionArgs args) => { return ((dynamic) args.Context).Name == "Test User"; };

			var process = new ProcessorArgs(tokens);
			process.Context = new {Name = "Wrong User"};

			StringBuilder result = processor.Process(process);
			Assert.AreEqual("", result.ToString());
		}

		[TestMethod]
		public void BasicForEachTokenWithProviderReturnBasedOnContext()
		{
			string templateText = "<%FOREACH LoopName%><%=Name%><%ENDFOR%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);
			var user1 = new TestUser {Name = "First Name"};
			var user2 = new TestUser {Name = "Last Name"};

			var processor = new Processor();

			processor.TokenValueProvider = (TokenArgs args) => ((TestUser) args.Context).Name;
			processor.LoopValueProvider = (LoopArgs args) =>
			{
				//in case of user1 context return user2 object.
				if (((TestUser) args.Context).Name == "First Name")
					return new List<TestUser> {user2};

				return new List<TestUser> {user1};
			};

			var process = new ProcessorArgs(tokens);
			process.Context = user1;
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("Last Name", result.ToString());

			process = new ProcessorArgs(tokens);
			process.Context = user2;

			result = processor.Process(process);
			Assert.AreEqual("First Name", result.ToString());
		}


		/*
		 * with change in default context
		 * with change in for context
		 * with and if condition context
		 * With in case of Null return will not render
		 */

		private string UserTokenDataProvider(TokenArgs args)
		{
			var arg = args.Context as TestUser;
			if (arg == null) return null;

			switch (args.Name)
			{
				case "name":
					return arg.Name;
			}
			return null;
		}

		private string ProfileTokenDataProvider(TokenArgs args)
		{
			var arg = args.Context as TestUserProfile;
			if (arg == null) return null;

			switch (args.Name)
			{
				case "address":
					return arg.Address;
			}
			return null;
		}

		private bool? UserConditionValueProvider(ConditionArgs args)
		{
			var arg = args.Context as TestUser;
			if (arg == null) return null;

			switch (args.Name)
			{
				case "hasname":
					return string.IsNullOrEmpty(arg.Name) == false;
			}
			return null;
		}

		private bool? ProfileConditionValueProvider(ConditionArgs args)
		{
			var arg = args.Context as TestUserProfile;
			if (arg == null) return null;

			switch (args.Name)
			{
				case "hasaddress":
					return string.IsNullOrEmpty(arg.Address) == false;
			}
			return null;
		}

		[TestMethod]
		public void WithTokenNoProvider()
		{
			string templateText = "Name:<%=Name%><%WITH Profile%>, Address: <%=Address%><%ENDWITH%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);
			var user = new TestUser {Name = "My User", Profile = new TestUserProfile {Address = "abcd"}};

			var processor = new Processor();
			processor.TokenValueProvider = (TokenArgs args) =>
			{
				string retVal = UserTokenDataProvider(args);
				if (retVal == null) retVal = ProfileTokenDataProvider(args);

				return retVal;
			};

			var process = new ProcessorArgs(tokens);
			process.Context = user;
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("Name:My User", result.ToString());
		}

		[TestMethod]
		public void WithTokenProviderButNullReturn()
		{
			string templateText = "Name:<%=Name%><%WITH Profile%>, Address: <%=Address%><%ENDWITH%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);
			var user = new TestUser {Name = "My User", Profile = new TestUserProfile {Address = "abcd"}};

			var processor = new Processor();
			processor.WithValueProvider = (TokenArgs args) => null;
			processor.TokenValueProvider = (TokenArgs args) =>
			{
				string retVal = UserTokenDataProvider(args);
				if (retVal == null) retVal = ProfileTokenDataProvider(args);

				return retVal;
			};

			var process = new ProcessorArgs(tokens);
			process.Context = user;
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("Name:My User", result.ToString());
		}

		[TestMethod]
		public void WithTokenValueOverrideDefaultContextNamedToken()
		{
			string templateText = "Name:<%=Name%><%WITH Profile%>, Address: <%=Address%><%ENDWITH%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);
			var user = new TestUser {Name = "My User", Profile = new TestUserProfile {Address = "abcd"}};

			var processor = new Processor();
			processor.WithValueProvider = (TokenArgs args) => ((TestUser) args.Context).Profile;
			processor.TokenValueProvider = (TokenArgs args) =>
			{
				string retVal = UserTokenDataProvider(args);
				if (retVal == null) retVal = ProfileTokenDataProvider(args);

				return retVal;
			};

			var process = new ProcessorArgs(tokens);
			process.Context = user;
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("Name:My User, Address: abcd", result.ToString());
		}

		[TestMethod]
		public void WithTokenValueOverrideDefaultContextConditionToken()
		{
			string templateText =
				"<%IF HasName THEN%>Name:<%=Name%><%ENDIF%><%WITH Profile%><%IF HasAddress THEN%>, Address: <%=Address%><%ENDIF%><%ENDWITH%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);
			var user = new TestUser {Name = "My User", Profile = new TestUserProfile {Address = "abcd"}};

			var processor = new Processor();
			processor.WithValueProvider = (TokenArgs args) => ((TestUser) args.Context).Profile;
			processor.ConditionValueProvider = (ConditionArgs args) =>
			{
				bool? retVal = UserConditionValueProvider(args);
				if (retVal == null) retVal = ProfileConditionValueProvider(args);

				return retVal != null && retVal.Value;
			};

			processor.TokenValueProvider = (TokenArgs args) =>
			{
				string retVal = UserTokenDataProvider(args);
				if (retVal == null) retVal = ProfileTokenDataProvider(args);

				return retVal;
			};

			var process = new ProcessorArgs(tokens);
			process.Context = user;
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("Name:My User, Address: abcd", result.ToString());
		}

		[TestMethod]
		public void WithTokenValueOverrideDefaultContextForEachToken()
		{
			string templateText =
				"<%FOREACH User%>Name:<%=Name%><%WITH Profile%><%IF HasAddress THEN%>, Address: <%=Address%><%ENDIF%><%ENDWITH%><%ENDFOR%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);
			var user = new TestUser {Name = "My User", Profile = new TestUserProfile {Address = "abcd"}};

			var processor = new Processor();
			processor.LoopValueProvider = (LoopArgs args) => new List<TestUser> {user};
			processor.WithValueProvider = (TokenArgs args) => ((TestUser) args.Context).Profile;
			processor.ConditionValueProvider = (ConditionArgs args) =>
			{
				bool? retVal = UserConditionValueProvider(args);
				if (retVal == null) retVal = ProfileConditionValueProvider(args);

				return retVal != null && retVal.Value;
			};

			processor.TokenValueProvider = (TokenArgs args) =>
			{
				string retVal = UserTokenDataProvider(args);
				if (retVal == null) retVal = ProfileTokenDataProvider(args);

				return retVal;
			};

			var process = new ProcessorArgs(tokens);
			process.Context = user;
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("Name:My User, Address: abcd", result.ToString());
		}


		/*
		 * reuse one level (2nd element) within for loop
		 * reuse outside loop
		 */

		[TestMethod]
		public void ReUseForEachWithinLoop()
		{
			string templateText = "<%FOREACH Users%><%=Name%> K:<%REUSEFOREACH Users Related%><%ENDFOR%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.TokenValueProvider = (TokenArgs args) =>
			{
				string retVal = UserTokenDataProvider(args);
				if (retVal == null) retVal = ProfileTokenDataProvider(args);

				return retVal;
			};

			processor.LoopValueProvider = (LoopArgs args) =>
			{
				switch (args.Name)
				{
					case "users":
						return new List<TestUser> {new TestUser {Name = "1"}, new TestUser {Name = "2"}};
					case "related":
						if (((TestUser) args.Context).Name == "2")
							return new List<TestUser> {new TestUser {Name = "3"}, new TestUser {Name = "4"}};
						break;
				}

				return null;
			};

			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("1 K:2 K:3 K:4 K:", result.ToString());
		}

		[TestMethod]
		public void ReUseForEachOutsideLoop()
		{
			string templateText = "<%FOREACH Users%><%=Name%><%ENDFOR%><%REUSEFOREACH Users Related%>";
			var parser = new Parser();
			List<IToken> tokens = parser.Parse(templateText);

			var processor = new Processor();
			processor.TokenValueProvider = (TokenArgs args) =>
			{
				string retVal = UserTokenDataProvider(args);
				if (retVal == null) retVal = ProfileTokenDataProvider(args);

				return retVal;
			};

			processor.LoopValueProvider = (LoopArgs args) =>
			{
				switch (args.Name)
				{
					case "users":
						return new List<TestUser> {new TestUser {Name = "1"}, new TestUser {Name = "2"}};
					case "related":
						return new List<TestUser> {new TestUser {Name = "3"}, new TestUser {Name = "4"}};
				}

				return null;
			};

			var process = new ProcessorArgs(tokens);
			StringBuilder result = processor.Process(process);
			Assert.AreEqual("1234", result.ToString());
		}
	}
}