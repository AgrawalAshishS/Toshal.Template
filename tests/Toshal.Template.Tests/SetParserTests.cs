using System;
using System.Collections.Generic;
using Xunit;

namespace Toshal.Template.Tests
{
    using Toshal.Template;
    using Toshal.Template.Exceptions;
    using Toshal.Template.Tokens;

    public class SetParserTests
    {
        [Fact]
        public void ItShouldRecognizeSet()
        {
            const string templateText = "<%SET MyTokenName %>MyValue<%ENDSET%><%=MyTokenName%>";
            var parser = new Parser();
            List<IToken> result = parser.Parse(templateText);

            Assert.Equal(2, result.Count);
            var token = (SetToken)result[0];
            Assert.Equal("MyTokenName".ToLower(), token.Name);
            Assert.Single(token.InnerTokens);

            var namedToken = (NamedToken)result[1];
            Assert.Equal("MyTokenName".ToLower(), namedToken.Name);
        }

        [Fact]
        public void EmptyValueShouldNotThrowError()
        {
            const string templateText = "<%SET MyTokenName %><%ENDSET%>";
            var parser = new Parser();
            List<IToken> result = parser.Parse(templateText);

            Assert.Single(result);
            var token = (SetToken)result[0];
            Assert.Equal("MyTokenName".ToLower(), token.Name);
            Assert.Empty(token.InnerTokens);
        }
    }
}
