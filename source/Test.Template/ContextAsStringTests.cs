using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Test.Template
{
    using Toshal.Template;
    using Toshal.Template.Exceptions;
    using Toshal.Template.Tokens;

    [TestFixture]
    public class ContextAsStringTests
    {
        [Test]
        public void ItShouldHandleSimpleText()
        {
            const string templateText = "<%CONTEXT_AS_STRING%>";
            var parser = new Parser();

            List<IToken> result = parser.Parse(templateText);
            Assert.AreEqual(1, result.Count);
            Assert.IsInstanceOf<ContextAsStringToken>(result[0]);
        }

        [Test]
        public void ItShouldThrowErrorOnUnKnownTag()
        {
            const string templateText = "Simple text <%CONTEXT_AS_STRING%>";
            var parser = new Parser();

            List<IToken> result = parser.Parse(templateText);
            Assert.AreEqual(2, result.Count);
            Assert.IsInstanceOf<ContentToken>(result[0]);
            Assert.IsInstanceOf<ContextAsStringToken>(result[1]);
        }

    }
}