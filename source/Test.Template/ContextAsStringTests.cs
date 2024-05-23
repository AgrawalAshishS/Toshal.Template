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
    public class ContextAsStringTests
    {
        [Test]
        public void ItShouldHandleSimpleText()
        {
            const string templateText = "<%CONTEXT_AS_STRING%>";
            var parser = new Parser();

            List<IToken> result = parser.Parse(templateText);
            ClassicAssert.AreEqual(1, result.Count);
            ClassicAssert.IsInstanceOf<ContextAsStringToken>(result[0]);
        }

        [Test]
        public void ItShouldThrowErrorOnUnKnownTag()
        {
            const string templateText = "Simple text <%CONTEXT_AS_STRING%>";
            var parser = new Parser();

            List<IToken> result = parser.Parse(templateText);
            ClassicAssert.AreEqual(2, result.Count);
            ClassicAssert.IsInstanceOf<ContentToken>(result[0]);
            ClassicAssert.IsInstanceOf<ContextAsStringToken>(result[1]);
        }

    }
}