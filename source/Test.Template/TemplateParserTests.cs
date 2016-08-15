using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Template
{
    using Toshal.Template;
    using Toshal.Template.Exceptions;
    using Toshal.Template.Tokens;

    [TestClass]
    public class TemplateParserTests
    {
        [TestMethod]
        public void ItShouldHandleSimpleText()
        {
            const string templateText = "Simple text";
            var parser = new Parser();

            List<IToken> result = parser.Parse(templateText);
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOfType(result[0], typeof(ContentToken));
            Assert.AreEqual(templateText, ((ContentToken)result[0]).Content);
        }

        [TestMethod]
        [ExpectedException(typeof(ParserException))]
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
            Assert.IsInstanceOfType(result[0], typeof(NamedToken));
        }

        [TestMethod]
        public void TokenNameShouldBeLowerCase()
        {
            var parser = new Parser();

            List<IToken> result = parser.Parse("<%=Name%>");
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("name", ((NamedToken)result[0]).Name);
        }

        [TestMethod]
        [ExpectedException(typeof(TokenMissingNameException))]
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
            Assert.AreEqual(part1, ((ContentToken)result[0]).Content);
            Assert.AreEqual(part3.ToLower(), ((NamedToken)result[1]).Name);
            Assert.AreEqual(part5, ((ContentToken)result[2]).Content);
            Assert.AreEqual(part7.ToLower(), ((NamedToken)result[3]).Name);
        }
    }
}