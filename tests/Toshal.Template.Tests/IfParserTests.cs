using System;
using System.Collections.Generic;
using Xunit;

namespace Toshal.Template.Tests
{
    using Toshal.Template;
    using Toshal.Template.Exceptions;
    using Toshal.Template.Tokens;

    public class IfParserTests
    {
        [Fact]
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
            Assert.Equal(part2.ToLower(), ((ConditionToken)result[0]).Name);
        }

        [Fact]
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
            Assert.Equal(part2.ToLower(), ((ConditionToken)result[0]).Name);
        }

        [Fact]
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
                    Assert.Equal("<%IF  %>", ex.Split!.Content);
                    throw;
                }
            });
        }

        [Fact]
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
            Assert.Single(result);

            var token = (ConditionToken)result[0];
            Assert.Equal(part2.ToLower(), token.Name);
            Assert.Single(token.InnerTokens);

            var content = (ContentToken)token.InnerTokens[0];
            Assert.Equal(part4, content.Content);
        }

        [Fact]
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
                    Assert.Equal("<%IF ConditionName THEN%>", ex.Split!.Content);
                    throw;
                }
            });

        }

        [Fact]
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
                    Assert.Equal("<%IF ConditionName THEN%>", ex.Split!.Content);
                    throw;
                }
            });

        }

        [Fact]
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
            Assert.Equal(2, result.Count);

            var token = (ConditionToken)result[0];
            Assert.Equal(part2.ToLower(), token.Name);
            Assert.Single(token.InnerTokens);

            var nestedIf = (ConditionToken)token.InnerTokens[0];
            Assert.Equal(part5.ToLower(), nestedIf.Name);
            Assert.Single(nestedIf.InnerTokens);

            var content = (ContentToken)nestedIf.InnerTokens[0];
            Assert.Equal(part7, content.Content);
        }

        [Fact]
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
                    Assert.Equal(part2.ToLower(), ex.TokenName);
                    throw;
                }
            });

        }

        [Fact]
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

            Assert.Single(result);

            var token = (ConditionToken)result[0];
            Assert.Equal(part2.ToLower(), token.Name);
            Assert.Single(token.InnerTokens);

            var content = (ContentToken)token.InnerTokens[0];
            Assert.Equal(part4, content.Content);
            Assert.Single(token.FalsePart!.InnerTokens);

            content = (ContentToken)token.FalsePart!.InnerTokens[0];
            Assert.Equal(part6, content.Content);
        }

        [Fact]
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

            Assert.Single(result);

            var token = (ConditionToken)result[0];
            Assert.Equal(part2.ToLower(), token.Name);
            Assert.Single(token.InnerTokens);

            var content = (ContentToken)token.InnerTokens[0];
            Assert.Equal(part4, content.Content);
            Assert.Single(token.FalsePart!.InnerTokens);

            content = (ContentToken)token.FalsePart!.InnerTokens[0];
            Assert.Equal(part6, content.Content);
        }

        [Fact]
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

            Assert.Single(result);

            var token = (ConditionToken)result[0];
            Assert.Equal("conditionname", token.Name);
            Assert.Single(token.InnerTokens);

            var content = (ContentToken)token.InnerTokens[0];
            Assert.Equal("Content within IF", content.Content);

            token = (ConditionToken)token.FalsePart!;
            Assert.Equal("elseif1", token.Name);
            Assert.Single(token.InnerTokens);

            content = (ContentToken)token.InnerTokens[0];
            Assert.Equal("1st content", content.Content);

            token = (ConditionToken)token.FalsePart!;
            Assert.Equal("elseif2", token.Name);
            Assert.Single(token.InnerTokens);

            content = (ContentToken)token.InnerTokens[0];
            Assert.Equal("2nd content", content.Content);

            token = (ConditionToken)token.FalsePart!;
            Assert.Equal("elseif3", token.Name);
            Assert.Single(token.InnerTokens);

            content = (ContentToken)token.InnerTokens[0];
            Assert.Equal("3rd content", content.Content);

            var elseToken = (ElseToken)token.FalsePart!;
            Assert.Equal(2, elseToken.InnerTokens.Count);

            content = (ContentToken)elseToken.InnerTokens[0];
            Assert.Equal("else content ", content.Content);

            Assert.Equal("something", ((NamedToken)elseToken.InnerTokens[1]).Name);
        }

        [Fact]
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
                    Assert.Equal("othercondition", ex.TokenName);
                    Assert.Equal("<%ELSEIF OtherCondition THEN%>", ex.Split!.Content);
                    throw;
                }
            });

        }

        [Fact]
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
                    Assert.Equal("conditionname", ex.TokenName);
                    Assert.Equal("<%IF ConditionName THEN%>", ex.Split!.Content);
                    throw;
                }
            });

        }

        [Fact]
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
            Assert.Equal(part2.ToLower(), ((ConditionToken)result[0]).Name);
            Assert.False(((ConditionToken)result[0]).IsPositive);
        }

        
    }
}