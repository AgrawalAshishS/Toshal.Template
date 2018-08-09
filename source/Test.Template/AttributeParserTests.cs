using System.Collections.Generic;
using NUnit.Framework;


namespace Test.Template
{
    using System.Text;
    using Toshal.Template;
    using Toshal.Template.Exceptions;
    using Toshal.Template.Tokens;

    [TestFixture]
    public class AttributeParserTests
    {
        [Test]
        public void ItShouldAbleToTakeBasicAttribute()
        {
            const string templateText = "<%=Token name=\"Value\" name2=\"value2\"%>";
            var parser = new Parser();
            List<IToken> result = parser.Parse(templateText);

            Assert.AreEqual(1, result.Count);
            var token = (NamedToken)result[0];
            Assert.AreEqual("token", token.Name);
            Assert.AreEqual(2, token.Attributes.Count);
            Assert.AreEqual("Value", token.Attributes["name"]);
            Assert.AreEqual("value2", token.Attributes["name2"]);
        }

        [Test]
        public void ItShouldThrowExceptionIfAttributeNotCorrect1()
        {
            const string templateText = "<%=Token name=Value%>";
            var parser = new Parser();

            Assert.Throws<InvalidTokenAttributeException>(() =>
            {
                parser.Parse(templateText);
            });
        }

        [Test]
        public void ItShouldThrowExceptionIfAttributeNotCorrect2()
        {
            const string templateText = "<%=Token name=\"Value%>";
            var parser = new Parser();
            Assert.Throws<InvalidTokenAttributeException>(() =>
            {
                parser.Parse(templateText);
            });
        }

        [Test]
        public void ItShouldThrowExceptionIfAttributeNotCorrect3()
        {
            const string templateText = "<%=Token name=Value\"%>";
            var parser = new Parser();
            Assert.Throws<InvalidTokenAttributeException>(() =>
            {
                parser.Parse(templateText);
            });
        }

        [Test]
        public void ItShouldThrowExceptionIfAttributeNotCorrect4()
        {
            const string templateText = "<%=Token name=%>";
            var parser = new Parser();
            Assert.Throws<InvalidTokenAttributeException>(() =>
            {
                parser.Parse(templateText);
            });

        }

        [Test]
        public void ItShouldThrowExceptionIfAttributeNotCorrect5()
        {
            const string templateText = "<%=Token =\"value\"%>";
            var parser = new Parser();
            Assert.Throws<InvalidTokenAttributeException>(() =>
            {
                parser.Parse(templateText);
            });
        }

        [Test]
        public void ItShouldThrowExceptionIfAttributeNotCorrect6()
        {
            const string templateText = "<%=Token name = \"value\"%>";
            var parser = new Parser();
            Assert.Throws<InvalidTokenAttributeException>(() =>
            {
                parser.Parse(templateText);
            });
        }

        [Test]
        public void ItShouldAbleToTakeBasicAttributeForWithToken()
        {
            const string templateText = "<%WITH WithName name=\"Value\" name2=\"value2\"%> some content <%ENDWITH%>";
            var parser = new Parser();
            List<IToken> result = parser.Parse(templateText);

            Assert.AreEqual(1, result.Count);
            var token = (WithToken)result[0];
            Assert.AreEqual("withname", token.Name);
            Assert.AreEqual(2, token.Attributes.Count);
            Assert.AreEqual("Value", token.Attributes["name"]);
            Assert.AreEqual("value2", token.Attributes["name2"]);
        }

        [Test]
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
            Assert.AreEqual("conditionname", ((ConditionToken)result[0]).Name);
            Assert.AreEqual(true, ((ConditionToken)result[0]).IsPositive);
            Assert.AreEqual(1, ((ConditionToken)result[0]).Attributes.Count);
        }

        [Test]
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
            Assert.AreEqual("conditionname", ((ConditionToken)result[0]).Name);
            Assert.AreEqual(false, ((ConditionToken)result[0]).IsPositive);
            Assert.AreEqual(1, ((ConditionToken)result[0]).Attributes.Count);
        }

        [Test]
        public void ItShouldHandleBasicForEach()
        {
            string templateText = "<%FOREACH Name abc=\"xyz\"%>";
            templateText += "Content within for";
            templateText += "<%ENDFOR%>";

            var parser = new Parser();
            List<IToken> result = parser.Parse(templateText);

            Assert.AreEqual(1, result.Count);
            var token = (ForEachToken)result[0];
            Assert.AreEqual("name", token.Name);
            Assert.AreEqual(1, token.Attributes.Count);
            Assert.AreEqual("xyz", token.Attributes.GetValue("abc", "pqr"));
            Assert.AreEqual(1, token.RowTokens.Count);

            var content = (ContentToken)token.RowTokens[0];
            Assert.AreEqual("Content within for", content.Content);
        }
    }
}