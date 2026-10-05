using System;
using System.Collections.Generic;
using Xunit;

namespace Toshal.Template.Tests
{
    using Toshal.Template;
    using Toshal.Template.Exceptions;
    using Toshal.Template.Tokens;

    public class WithParserTests
    {
        [Fact]
        public void ItShouldHandleBasicWithStatement()
        {
            const string templateText = "<%WITH Name %>Some content <%=SomeToken%><%ENDWITH%>";
            var parser = new Parser();
            List<IToken> result = parser.Parse(templateText);

            Assert.Equal(1, result.Count);
            var token = (WithToken)result[0];
            Assert.Equal("name", token.Name);

            Assert.Equal(2, token.InnerTokens.Count);
            var content = (ContentToken)token.InnerTokens[0];
            Assert.Equal("Some content ", content.Content);

            var namedContent = (NamedToken)token.InnerTokens[1];
            Assert.Equal("sometoken", namedContent.Name);
        }

        [Fact]
        public void ItShouldThrowExceptionForMissingNameInWith()
        {
            const string templateText = "<%WITH %>Some content<%ENDWITH%>";
            var parser = new Parser();

            Assert.Throws<TokenMissingNameException>(() =>
            {
                parser.Parse(templateText);
            });
        }

        [Fact]
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