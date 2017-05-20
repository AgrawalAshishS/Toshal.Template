using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Test.Template
{
    using Toshal.Template;
    using Toshal.Template.Exceptions;
    using Toshal.Template.Tokens;

    [TestFixture]
    public class SetParserTests
    {
        [Test]
        public void ItShouldRecognizeSet()
        {
            const string templateText = "<%SET MyTokenName MyValue %><%=MyTokenName%>";
            var parser = new Parser();
            List<IToken> result = parser.Parse(templateText);

            Assert.AreEqual(2, result.Count);
            var token = (SetToken)result[0];
            Assert.AreEqual("MyTokenName".ToLower(), token.Name);
            Assert.AreEqual("MyValue", token.Value);

            var namedToken = (NamedToken)result[1];
            Assert.AreEqual("MyTokenName".ToLower(), namedToken.Name);
        }

        [Test]
        public void EmptyValueShouldNotThrowError()
        {
            const string templateText = "<%SET MyTokenName %>";
            var parser = new Parser();
            List<IToken> result = parser.Parse(templateText);

            Assert.AreEqual(1, result.Count);
            var token = (SetToken)result[0];
            Assert.AreEqual("MyTokenName".ToLower(), token.Name);
            Assert.AreEqual("", token.Value);
        }
    }
}
