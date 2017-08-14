using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Test.Template
{
    using Toshal.Template;
    using Toshal.Template.Exceptions;
    using Toshal.Template.Tokens;

    [TestFixture]
    public class ReuseForEachParserTests
    {
        [Test]
        public void ItShouldHandleBasicReuseForEachToken()
        {
            const string templateText =
                "<%FOREACH ExistingForEachName %><%ENDFOR%><%REUSE_FOREACH ExistingForEachName NewLoopName %>";
            var parser = new Parser();
            List<IToken> result = parser.Parse(templateText);

            Assert.AreEqual(2, result.Count);
            var token = (ReuseForEachToken)result[1];
            Assert.AreEqual("newloopname", token.Name);
            Assert.AreEqual("existingforeachname", token.ExistingForEachName);
        }

        [Test]
        public void ItShouldHandleBasicReuseWithInForEachToken()
        {
            const string templateText =
                "<%FOREACH ExistingForEachName %><%REUSE_FOREACH ExistingForEachName NewLoopName %><%ENDFOR%>";
            var parser = new Parser();
            List<IToken> result = parser.Parse(templateText);

            Assert.AreEqual(1, result.Count);
            var forToken = (ForEachToken)result[0];
            var token = (ReuseForEachToken)forToken.RowTokens[0];
            Assert.AreEqual("newloopname", token.Name);
            Assert.AreEqual("existingforeachname", token.ExistingForEachName);
        }

        [Test]
        public void ItShouldThrowExceptionForMissingNameForReuseForEach()
        {
            const string templateText = "<%REUSE_FOREACH ExistingForEachName %>";
            var parser = new Parser();
            Assert.Throws<TokenMissingNameException>(() =>
            {
                parser.Parse(templateText);
            });
            
        }

        [Test]
        public void ItShouldThrowExceptionForMissingLoopNameForReuseForEach()
        {
            const string templateText = "<%REUSE_FOREACH %>";
            var parser = new Parser();
            Assert.Throws<TokenMissingNameException>(() =>
            {
                parser.Parse(templateText);
            });
        }

        [Test]
        public void ItShouldThrowExceptionIfGivenForEachNameDoesntPreeceedReuse()
        {
            const string templateText = "<%REUSE_FOREACH ExistingForEachName NewLoopName %>";
            var parser = new Parser();
            Assert.Throws<ForEachMissingForReuseException>(() =>
            {
                parser.Parse(templateText);
            });
        }

        [Test]
        public void ItShouldThrowExceptionIfGivenForEachNameDoesntPreeceedReuse2()
        {
            const string templateText = "<%FOREACH SomeName%><%ENDFOR%><%REUSE_FOREACH ExistingForEachName NewLoopName %>";
            var parser = new Parser();
            Assert.Throws<ForEachMissingForReuseException>(() =>
            {
                parser.Parse(templateText);
            });
        }
    }
}