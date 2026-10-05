using System;
using System.Collections.Generic;

using Xunit;

namespace Toshal.Template.Tests
{
    using Toshal.Template;
    using Toshal.Template.Exceptions;
    using Toshal.Template.Tokens;

    public class ContextAsStringTests
    {
        [Fact]
        public void ItShouldHandleSimpleText()
        {
            const string templateText = "<%CONTEXT_AS_STRING%>";
            var parser = new Parser();

            List<IToken> result = parser.Parse(templateText);
            Assert.Equal(1, result.Count);
            Assert.IsAssignableFrom<ContextAsStringToken>(result[0]);
        }

        [Fact]
        public void ItShouldThrowErrorOnUnKnownTag()
        {
            const string templateText = "Simple text <%CONTEXT_AS_STRING%>";
            var parser = new Parser();

            List<IToken> result = parser.Parse(templateText);
            Assert.Equal(2, result.Count);
            Assert.IsAssignableFrom<ContentToken>(result[0]);
            Assert.IsAssignableFrom<ContextAsStringToken>(result[1]);
        }

    }
}