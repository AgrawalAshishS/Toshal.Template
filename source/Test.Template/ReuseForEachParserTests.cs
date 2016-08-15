using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test.Template
{
    using Toshal.Template;
    using Toshal.Template.Exceptions;
    using Toshal.Template.Tokens;

    [TestClass]
    public class ReuseForEachParserTests
    {
        [TestMethod]
        public void ItShouldHandleBasicReuseForEachToken()
        {
            const string templateText =
                "<%FOREACH ExistingForEachName %><%ENDFOR%><%REUSEFOREACH ExistingForEachName NewLoopName %>";
            var parser = new Parser();
            List<IToken> result = parser.Parse(templateText);

            Assert.AreEqual(2, result.Count);
            var token = (ReuseForEachToken)result[1];
            Assert.AreEqual("newloopname", token.Name);
            Assert.AreEqual("existingforeachname", token.ExistingForEachName);
        }

        [TestMethod]
        public void ItShouldHandleBasicReuseWithInForEachToken()
        {
            const string templateText =
                "<%FOREACH ExistingForEachName %><%REUSEFOREACH ExistingForEachName NewLoopName %><%ENDFOR%>";
            var parser = new Parser();
            List<IToken> result = parser.Parse(templateText);

            Assert.AreEqual(1, result.Count);
            var forToken = (ForEachToken)result[0];
            var token = (ReuseForEachToken)forToken.RowTokens[0];
            Assert.AreEqual("newloopname", token.Name);
            Assert.AreEqual("existingforeachname", token.ExistingForEachName);
        }

        [TestMethod]
        [ExpectedException(typeof(TokenMissingNameException))]
        public void ItShouldThrowExceptionForMissingNameForReuseForEach()
        {
            const string templateText = "<%REUSEFOREACH ExistingForEachName %>";
            var parser = new Parser();
            parser.Parse(templateText);
        }

        [TestMethod]
        [ExpectedException(typeof(TokenMissingNameException))]
        public void ItShouldThrowExceptionForMissingLoopNameForReuseForEach()
        {
            const string templateText = "<%REUSEFOREACH %>";
            var parser = new Parser();
            parser.Parse(templateText);
        }

        [TestMethod]
        [ExpectedException(typeof(ForEachMissingForReuseException))]
        public void ItShouldThrowExceptionIfGivenForEachNameDoesntPreeceedReuse()
        {
            const string templateText = "<%REUSEFOREACH ExistingForEachName NewLoopName %>";
            var parser = new Parser();
            parser.Parse(templateText);
        }

        [TestMethod]
        [ExpectedException(typeof(ForEachMissingForReuseException))]
        public void ItShouldThrowExceptionIfGivenForEachNameDoesntPreeceedReuse2()
        {
            const string templateText = "<%FOREACH SomeName%><%ENDFOR%><%REUSEFOREACH ExistingForEachName NewLoopName %>";
            var parser = new Parser();
            parser.Parse(templateText);
        }
    }
}