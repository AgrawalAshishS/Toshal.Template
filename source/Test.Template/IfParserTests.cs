using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Test.Template
{
    using Toshal.Template;
    using Toshal.Template.Exceptions;
    using Toshal.Template.Tokens;

    [TestFixture]
    public class IfParserTests
    {
        [Test]
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
            Assert.AreEqual(part2.ToLower(), ((ConditionToken)result[0]).Name);
        }

        [Test]
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
            Assert.AreEqual(part2.ToLower(), ((ConditionToken)result[0]).Name);
        }

        [Test]
        public void IfWithoutConditionNameShouldThrowException()
        {
            string part1 = "<%IF ";
            string part2 = "";
            string part3 = " %>";
            string part4 = "Content within IF";
            string part5 = "<%ENDIF%>";

            string templateText = part1 + part2 + part3 + part4 + part5;

            var parser = new Parser();
            Assert.Throws<TokenMissingNameException>(() =>
            {
                try
                {
                    parser.Parse(templateText);
                }
                catch (TokenMissingNameException ex)
                {
                    Assert.AreEqual("<%IF  %>", ex.Split.Content);
                    throw;
                }
            });
        }

        [Test]
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

            var token = (ConditionToken)result[0];
            Assert.AreEqual(part2.ToLower(), token.Name);
            Assert.AreEqual(1, token.InnerTokens.Count);

            var content = (ContentToken)token.InnerTokens[0];
            Assert.AreEqual(part4, content.Content);
        }

        [Test]
        public void IfWithoutEndIfShouldThrowException()
        {
            string part1 = "<%IF ";
            string part2 = "ConditionName";
            string part3 = " THEN%>";
            string part4 = "Content within IF";

            string templateText = part1 + part2 + part3 + part4;

            var parser = new Parser();
            Assert.Throws<TokenNotClosedException>(() =>
            {

                try
                {
                    parser.Parse(templateText);
                }
                catch (TokenNotClosedException ex)
                {
                    Assert.AreEqual("<%IF ConditionName THEN%>", ex.Split.Content);
                    throw;
                }
            });

        }

        [Test]
        public void IfWithoutEndIfShouldThrowException2()
        {
            string part1 = "<%IF ";
            string part2 = "ConditionName";
            string part3 = " THEN%>";
            string part4 = "Content within IF<%%>";

            string templateText = part1 + part2 + part3 + part4;

            var parser = new Parser();
            Assert.Throws<TokenNotClosedException>(() =>
            {
                try
                {
                    parser.Parse(templateText);
                }
                catch (TokenNotClosedException ex)
                {
                    Assert.AreEqual("<%IF ConditionName THEN%>", ex.Split.Content);
                    throw;
                }
            });

        }

        [Test]
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

            var token = (ConditionToken)result[0];
            Assert.AreEqual(part2.ToLower(), token.Name);
            Assert.AreEqual(1, token.InnerTokens.Count);

            var nestedIf = (ConditionToken)token.InnerTokens[0];
            Assert.AreEqual(part5.ToLower(), nestedIf.Name);
            Assert.AreEqual(1, nestedIf.InnerTokens.Count);

            var content = (ContentToken)nestedIf.InnerTokens[0];
            Assert.AreEqual(part7, content.Content);
        }

        [Test]
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
            Assert.Throws<TokenNotClosedException>(() =>
            {
                try
                {
                    parser.Parse(templateText);
                }
                catch (TokenNotClosedException ex)
                {
                    Assert.AreEqual(part2.ToLower(), ex.TokenName);
                    throw;
                }
            });

        }

        [Test]
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

            var token = (ConditionToken)result[0];
            Assert.AreEqual(part2.ToLower(), token.Name);
            Assert.AreEqual(1, token.InnerTokens.Count);

            var content = (ContentToken)token.InnerTokens[0];
            Assert.AreEqual(part4, content.Content);
            Assert.AreEqual(1, token.FalsePart.InnerTokens.Count);

            content = (ContentToken)token.FalsePart.InnerTokens[0];
            Assert.AreEqual(part6, content.Content);
        }

        [Test]
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

            var token = (ConditionToken)result[0];
            Assert.AreEqual(part2.ToLower(), token.Name);
            Assert.AreEqual(1, token.InnerTokens.Count);

            var content = (ContentToken)token.InnerTokens[0];
            Assert.AreEqual(part4, content.Content);
            Assert.AreEqual(1, token.FalsePart.InnerTokens.Count);

            content = (ContentToken)token.FalsePart.InnerTokens[0];
            Assert.AreEqual(part6, content.Content);
        }

        [Test]
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

            var token = (ConditionToken)result[0];
            Assert.AreEqual("conditionname", token.Name);
            Assert.AreEqual(1, token.InnerTokens.Count);

            var content = (ContentToken)token.InnerTokens[0];
            Assert.AreEqual("Content within IF", content.Content);

            token = (ConditionToken)token.FalsePart;
            Assert.AreEqual("elseif1", token.Name);
            Assert.AreEqual(1, token.InnerTokens.Count);

            content = (ContentToken)token.InnerTokens[0];
            Assert.AreEqual("1st content", content.Content);

            token = (ConditionToken)token.FalsePart;
            Assert.AreEqual("elseif2", token.Name);
            Assert.AreEqual(1, token.InnerTokens.Count);

            content = (ContentToken)token.InnerTokens[0];
            Assert.AreEqual("2nd content", content.Content);

            token = (ConditionToken)token.FalsePart;
            Assert.AreEqual("elseif3", token.Name);
            Assert.AreEqual(1, token.InnerTokens.Count);

            content = (ContentToken)token.InnerTokens[0];
            Assert.AreEqual("3rd content", content.Content);

            var elseToken = (ElseToken)token.FalsePart;
            Assert.AreEqual(2, elseToken.InnerTokens.Count);

            content = (ContentToken)elseToken.InnerTokens[0];
            Assert.AreEqual("else content ", content.Content);

            Assert.AreEqual("something", ((NamedToken)elseToken.InnerTokens[1]).Name);
        }

        [Test]
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
            Assert.Throws<TokenNotClosedException>(() =>
            {
                try
                {
                    parser.Parse(templateText);
                }
                catch (TokenNotClosedException ex)
                {
                    Assert.AreEqual("othercondition", ex.TokenName);
                    Assert.AreEqual("<%ELSEIF OtherCondition THEN%>", ex.Split.Content);
                    throw;
                }
            });

        }

        [Test]
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
            Assert.Throws<TokenNotClosedException>(() =>
            {
                try
                {
                    parser.Parse(templateText);
                }
                catch (TokenNotClosedException ex)
                {
                    Assert.AreEqual("conditionname", ex.TokenName);
                    Assert.AreEqual("<%IF ConditionName THEN%>", ex.Split.Content);
                    throw;
                }
            });

        }

        [Test]
        public void ItShouldHandleNegativeConditionToken()
        {
            string part1 = "<%IF NOT ";
            string part2 = "ConditionName";
            string part3 = " THEN%>";
            string part4 = "Content within IF";
            string part5 = "<%ENDIF%>";

            string templateText = part1 + part2 + part3 + part4 + part5;

            var parser = new Parser();

            List<IToken> result = parser.Parse(templateText);
            Assert.AreEqual(part2.ToLower(), ((ConditionToken)result[0]).Name);
            Assert.AreEqual(false, ((ConditionToken)result[0]).IsPositive);
        }
    }
}