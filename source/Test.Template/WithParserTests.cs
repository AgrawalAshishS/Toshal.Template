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
    public class WithParserTests
    {
        [Test]
        public void ItShouldHandleBasicWithStatement()
        {
            const string templateText = "<%WITH Name %>Some content <%=SomeToken%><%ENDWITH%>";
            var parser = new Parser();
            List<IToken> result = parser.Parse(templateText);

            ClassicAssert.AreEqual(1, result.Count);
            var token = (WithToken)result[0];
            ClassicAssert.AreEqual("name", token.Name);

            ClassicAssert.AreEqual(2, token.InnerTokens.Count);
            var content = (ContentToken)token.InnerTokens[0];
            ClassicAssert.AreEqual("Some content ", content.Content);

            var namedContent = (NamedToken)token.InnerTokens[1];
            ClassicAssert.AreEqual("sometoken", namedContent.Name);
        }

        [Test]
        public void ItShouldThrowExceptionForMissingNameInWith()
        {
            const string templateText = "<%WITH %>Some content<%ENDWITH%>";
            var parser = new Parser();

            Assert.Throws<TokenMissingNameException>(() =>
            {
                parser.Parse(templateText);
            });
        }

        [Test]
        public void ItShouldThrowExceptionForMissingEndForWith()
        {
            const string templateText = "<%WITH Name %>Some content";
            var parser = new Parser();
            Assert.Throws<TokenNotClosedException>(() =>
            {
                parser.Parse(templateText);
            });
        }
    }
}