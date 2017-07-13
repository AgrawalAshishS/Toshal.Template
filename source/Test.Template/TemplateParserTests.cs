using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Test.Template
{
    using Toshal.Template;
    using Toshal.Template.Exceptions;
    using Toshal.Template.Tokens;

    [TestFixture]
    public class TemplateParserTests
    {
        [Test]
        public void ItShouldHandleSimpleText()
        {
            const string templateText = "Simple text";
            var parser = new Parser();

            List<IToken> result = parser.Parse(templateText);
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOf<ContentToken>(result[0]);
            Assert.AreEqual(templateText, ((ContentToken)result[0]).Content);
        }

        [Test]
        public void ItShouldThrowErrorOnUnKnownTag()
        {
            const string templateText = "Simple text<%%>";
            var parser = new Parser();

            Assert.Throws<ParserException>(() =>
            {
                try
                {
                    parser.Parse(templateText);
                }
                catch (ParserException ex)
                {
                    Assert.AreEqual("<%%>", ex.Split.Content);
                    throw;
                }
            });
        }

        [Test]
        public void ItShouldHandleBasicToken()
        {
            var parser = new Parser();

            List<IToken> result = parser.Parse("<%=Name%>");
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOf<NamedToken>(result[0]);
        }

        [Test]
        public void TokenNameShouldBeLowerCase()
        {
            var parser = new Parser();

            List<IToken> result = parser.Parse("<%=Name%>");
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("name", ((NamedToken)result[0]).Name);
        }

        [Test]
        public void TokenNameRequired()
        {
            var parser = new Parser();
            Assert.Throws<TokenMissingNameException>(() =>
            {
                try
                {
                    parser.Parse("<%=%>");
                }
                catch (TokenMissingNameException ex)
                {
                    Assert.AreEqual("<%=%>", ex.Split.Content);
                    throw;
                }
            });

        }

        [Test]
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