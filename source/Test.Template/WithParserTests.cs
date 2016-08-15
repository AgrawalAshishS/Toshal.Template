using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Template
{
    using Toshal.Template;
    using Toshal.Template.Exceptions;
    using Toshal.Template.Tokens;

    [TestClass]
    public class WithParserTests
    {
        [TestMethod]
        public void ItShouldHandleBasicWithStatement()
        {
            const string templateText = "<%WITH Name %>Some content <%=SomeToken%><%ENDWITH%>";
            var parser = new Parser();
            List<IToken> result = parser.Parse(templateText);

            Assert.AreEqual(1, result.Count);
            var token = (WithToken)result[0];
            Assert.AreEqual("name", token.Name);

            Assert.AreEqual(2, token.InnerTokens.Count);
            var content = (ContentToken)token.InnerTokens[0];
            Assert.AreEqual("Some content ", content.Content);

            var namedContent = (NamedToken)token.InnerTokens[1];
            Assert.AreEqual("sometoken", namedContent.Name);
        }

        [TestMethod]
        [ExpectedException(typeof(TokenMissingNameException))]
        public void ItShouldThrowExceptionForMissingNameInWith()
        {
            const string templateText = "<%WITH %>Some content<%ENDWITH%>";
            var parser = new Parser();
            parser.Parse(templateText);
        }

        [TestMethod]
        [ExpectedException(typeof(TokenNotClosedException))]
        public void ItShouldThrowExceptionForMissingEndForWith()
        {
            const string templateText = "<%WITH Name %>Some content";
            var parser = new Parser();
            parser.Parse(templateText);
        }
    }
}