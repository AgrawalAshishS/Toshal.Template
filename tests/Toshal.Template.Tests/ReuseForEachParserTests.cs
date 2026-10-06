using System;
using System.Collections.Generic;

using Xunit;

namespace Toshal.Template.Tests
{
    using Toshal.Template;
    using Toshal.Template.Exceptions;
    using Toshal.Template.Tokens;

    public class ReuseForEachParserTests
    {
        [Fact]
        public void ItShouldHandleBasicReuseForEachToken()
        {
            const string templateText =
                "<%FOREACH ExistingForEachName %><%ENDFOR%><%REUSE_FOREACH ExistingForEachName NewLoopName %>";
            var parser = new Parser();
            List<IToken> result = parser.Parse(templateText);

            Assert.Equal(2, result.Count);
            var token = (ReuseForEachToken)result[1];
            Assert.Equal("newloopname", token.Name);
            Assert.Equal("existingforeachname", token.ExistingForEachName);
        }

        [Fact]
        public void ItShouldHandleBasicReuseWithInForEachToken()
        {
            const string templateText =
                "<%FOREACH ExistingForEachName %><%REUSE_FOREACH ExistingForEachName NewLoopName %><%ENDFOR%>";
            var parser = new Parser();
            List<IToken> result = parser.Parse(templateText);

            Assert.Single(result);
            var forToken = (ForEachToken)result[0];
            var token = (ReuseForEachToken)forToken.RowTokens[0];
            Assert.Equal("newloopname", token.Name);
            Assert.Equal("existingforeachname", token.ExistingForEachName);
        }

        [Fact]
        public void ItShouldThrowExceptionForMissingNameForReuseForEach()
        {
            const string templateText = "<%REUSE_FOREACH ExistingForEachName %>";
            var parser = new Parser();
            Assert.Throws<TokenMissingNameException>(() =>
            {
                parser.Parse(templateText);
            });

        }

        [Fact]
        public void ItShouldThrowExceptionForMissingLoopNameForReuseForEach()
        {
            const string templateText = "<%REUSE_FOREACH %>";
            var parser = new Parser();
            Assert.Throws<TokenMissingNameException>(() =>
            {
                parser.Parse(templateText);
            });
        }

        [Fact]
        public void ItShouldThrowExceptionIfGivenForEachNameDoesntPreeceedReuse()
        {
            const string templateText = "<%REUSE_FOREACH ExistingForEachName NewLoopName %>";
            var parser = new Parser();
            Assert.Throws<ForEachMissingForReuseException>(() =>
            {
                parser.Parse(templateText);
            });
        }

        [Fact]
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