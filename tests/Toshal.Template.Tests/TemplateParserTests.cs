using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Test.Template
{
    using NUnit.Framework.Legacy;
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
            ClassicAssert.AreEqual(1, result.Count);
            ClassicAssert.IsInstanceOf<ContentToken>(result[0]);
            ClassicAssert.AreEqual(templateText, ((ContentToken)result[0]).Content);
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
                    ClassicAssert.AreEqual("<%%>", ex.Split.Content);
                    throw;
                }
            });
        }

        [Test]
        public void ItShouldHandleBasicToken()
        {
            var parser = new Parser();

            List<IToken> result = parser.Parse("<%=Name%>");
            ClassicAssert.AreEqual(1, result.Count);
            ClassicAssert.IsInstanceOf<NamedToken>(result[0]);
        }

        [Test]
        public void TokenNameShouldBeLowerCase()
        {
            var parser = new Parser();

            List<IToken> result = parser.Parse("<%=Name%>");
            ClassicAssert.AreEqual(1, result.Count);
            ClassicAssert.AreEqual("name", ((NamedToken)result[0]).Name);
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
                    ClassicAssert.AreEqual("<%=%>", ex.Split.Content);
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
            ClassicAssert.AreEqual(4, result.Count);
            ClassicAssert.AreEqual(part1, ((ContentToken)result[0]).Content);
            ClassicAssert.AreEqual(part3.ToLower(), ((NamedToken)result[1]).Name);
            ClassicAssert.AreEqual(part5, ((ContentToken)result[2]).Content);
            ClassicAssert.AreEqual(part7.ToLower(), ((NamedToken)result[3]).Name);
        }

        [Test]
        public void ItSouldRespectRegularAngularBreackts()
        {
            const string templateText = "Simple text< %";
            var parser = new Parser();

            List<IToken> result = parser.Parse(templateText);
            ClassicAssert.AreEqual(1, result.Count);
            ClassicAssert.IsInstanceOf<ContentToken>(result[0]);
            ClassicAssert.AreEqual(templateText, ((ContentToken)result[0]).Content);
        }

        [Test]
        public void ItSouldRespectRegularAngularBreackts2()
        {
            const string templateText = "Simple text<T>";
            var parser = new Parser();

            List<IToken> result = parser.Parse(templateText);
            ClassicAssert.AreEqual(1, result.Count);
            ClassicAssert.IsInstanceOf<ContentToken>(result[0]);
            ClassicAssert.AreEqual(templateText, ((ContentToken)result[0]).Content);
        }

        [Test]
        public void ItSouldRespectRegularAngularBreackts3()
        {
            const string templateText = "Simple text%%%%%";
            var parser = new Parser();

            List<IToken> result = parser.Parse(templateText);
            ClassicAssert.AreEqual(1, result.Count);
            ClassicAssert.IsInstanceOf<ContentToken>(result[0]);
            ClassicAssert.AreEqual(templateText, ((ContentToken)result[0]).Content);
        }
    }
}