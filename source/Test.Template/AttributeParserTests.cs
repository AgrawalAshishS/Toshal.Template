using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Template
{
    using Toshal.Template;
    using Toshal.Template.Exceptions;
    using Toshal.Template.Tokens;

    [TestClass]
    public class AttributeParserTests
    {
        [TestMethod]
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

        [TestMethod]
        [ExpectedException(typeof(InvalidTokenAttributeException))]
        public void ItShouldThrowExceptionIfAttributeNotCorrect1()
        {
            const string templateText = "<%=Token name=Value%>";
            var parser = new Parser();
            parser.Parse(templateText);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidTokenAttributeException))]
        public void ItShouldThrowExceptionIfAttributeNotCorrect2()
        {
            const string templateText = "<%=Token name=\"Value%>";
            var parser = new Parser();
            parser.Parse(templateText);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidTokenAttributeException))]
        public void ItShouldThrowExceptionIfAttributeNotCorrect3()
        {
            const string templateText = "<%=Token name=Value\"%>";
            var parser = new Parser();
            parser.Parse(templateText);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidTokenAttributeException))]
        public void ItShouldThrowExceptionIfAttributeNotCorrect4()
        {
            const string templateText = "<%=Token name=%>";
            var parser = new Parser();
            parser.Parse(templateText);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidTokenAttributeException))]
        public void ItShouldThrowExceptionIfAttributeNotCorrect5()
        {
            const string templateText = "<%=Token =\"value\"%>";
            var parser = new Parser();
            parser.Parse(templateText);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidTokenAttributeException))]
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
            var token = (WithToken)result[0];
            Assert.AreEqual("withname", token.Name);
            Assert.AreEqual(2, token.Attributes.Count);
            Assert.AreEqual("Value", token.Attributes["name"]);
            Assert.AreEqual("value2", token.Attributes["name2"]);
        }
    }
}