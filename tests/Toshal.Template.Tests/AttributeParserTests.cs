using System.Collections.Generic;
using Xunit;


namespace Toshal.Template.Tests
{
    using System.Text;
    using Toshal.Template;
    using Toshal.Template.Exceptions;
    using Toshal.Template.Tokens;

    public class AttributeParserTests
    {
        [Fact]
        public void ItShouldAbleToTakeBasicAttribute()
        {
            const string templateText = "<%=Token name=\"Value\" name2=\"value2\"%>";
            var parser = new Parser();
            List<IToken> result = parser.Parse(templateText);

            Assert.Single(result);
            var token = (NamedToken)result[0];
            Assert.Equal("token", token.Name);
            Assert.Equal(2, token.Attributes.Count);
            Assert.Equal("Value", token.Attributes["name"]);
            Assert.Equal("value2", token.Attributes["name2"]);
        }

        [Fact]
        public void ItShouldThrowExceptionIfAttributeNotCorrect1()
        {
            const string templateText = "<%=Token name=Value%>";
            var parser = new Parser();

            Assert.Throws<InvalidTokenAttributeException>(() =>
            {
                parser.Parse(templateText);
            });
        }

        [Fact]
        public void ItShouldThrowExceptionIfAttributeNotCorrect2()
        {
            const string templateText = "<%=Token name=\"Value%>";
            var parser = new Parser();
            Assert.Throws<InvalidTokenAttributeException>(() =>
            {
                parser.Parse(templateText);
            });
        }

        [Fact]
        public void ItShouldThrowExceptionIfAttributeNotCorrect3()
        {
            const string templateText = "<%=Token name=Value\"%>";
            var parser = new Parser();
            Assert.Throws<InvalidTokenAttributeException>(() =>
            {
                parser.Parse(templateText);
            });
        }

        [Fact]
        public void ItShouldThrowExceptionIfAttributeNotCorrect4()
        {
            const string templateText = "<%=Token name=%>";
            var parser = new Parser();
            Assert.Throws<InvalidTokenAttributeException>(() =>
            {
                parser.Parse(templateText);
            });

        }

        [Fact]
        public void ItShouldThrowExceptionIfAttributeNotCorrect5()
        {
            const string templateText = "<%=Token =\"value\"%>";
            var parser = new Parser();
            Assert.Throws<InvalidTokenAttributeException>(() =>
            {
                parser.Parse(templateText);
            });
        }

        [Fact]
        public void ItShouldThrowExceptionIfAttributeNotCorrect6()
        {
            const string templateText = "<%=Token name = \"value\"%>";
            var parser = new Parser();
            Assert.Throws<InvalidTokenAttributeException>(() =>
            {
                parser.Parse(templateText);
            });
        }

        [Fact]
        public void ItShouldAbleToTakeBasicAttributeForWithToken()
        {
            const string templateText = "<%WITH WithName name=\"Value\" name2=\"value2\"%> some content <%ENDWITH%>";
            var parser = new Parser();
            List<IToken> result = parser.Parse(templateText);

            Assert.Single(result);
            var token = (WithToken)result[0];
            Assert.Equal("withname", token.Name);
            Assert.Equal(2, token.Attributes.Count);
            Assert.Equal("Value", token.Attributes["name"]);
            Assert.Equal("value2", token.Attributes["name2"]);
        }

        [Fact]
        public void AttributesShouldBeAvailbalePositive()
        {
            string part1 = "<%IF ";
            string part2 = "ConditionName abc=\"xyz\"";
            string part3 = " THEN%>";
            string part4 = "Content within IF";
            string part5 = "<%ENDIF%>";

            string templateText = part1 + part2 + part3 + part4 + part5;

            var parser = new Parser();

            List<IToken> result = parser.Parse(templateText);
            Assert.Equal("conditionname", ((ConditionToken)result[0]).Name);
            Assert.True(((ConditionToken)result[0]).IsPositive);
            Assert.Single(((ConditionToken)result[0]).Attributes);
        }

        [Fact]
        public void AttributesShouldBeAvailbaleNegative()
        {
            string part1 = "<%IF NOT ";
            string part2 = "ConditionName abc=\"xyz\"";
            string part3 = " THEN%>";
            string part4 = "Content within IF";
            string part5 = "<%ENDIF%>";

            string templateText = part1 + part2 + part3 + part4 + part5;

            var parser = new Parser();

            List<IToken> result = parser.Parse(templateText);
            Assert.Equal("conditionname", ((ConditionToken)result[0]).Name);
            Assert.False(((ConditionToken)result[0]).IsPositive);
            Assert.Single(((ConditionToken)result[0]).Attributes);
        }

        [Fact]
        public void ItShouldHandleBasicForEach()
        {
            string templateText = "<%FOREACH Name abc=\"xyz\"%>";
            templateText += "Content within for";
            templateText += "<%ENDFOR%>";

            var parser = new Parser();
            List<IToken> result = parser.Parse(templateText);

            Assert.Single(result);
            var token = (ForEachToken)result[0];
            Assert.Equal("name", token.Name);
            Assert.Single(token.Attributes);
            Assert.Equal("xyz", token.Attributes.GetValue("abc", "pqr"));
            Assert.Single(token.RowTokens);

            var content = (ContentToken)token.RowTokens[0];
            Assert.Equal("Content within for", content.Content);
        }
    }
}