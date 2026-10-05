using Toshal.Template.Exceptions;
using Toshal.Template.Tokens;
using Xunit;

namespace Toshal.Template.Tests.Fixes
{
    // A template that stops right after <%ELSE%> must fail with TokenNotClosedException, like any other missing ENDIF.
    // Before the fix it failed with ArgumentOutOfRangeException.
    // The ElseToken must also carry the position of the <%ELSE%> tag, not of the text after it.
    public class ElseAtEndTests
    {
        [Fact]
        public void TemplateEndingWithElseThrowsTokenNotClosed()
        {
            var ex = Assert.Throws<TokenNotClosedException>(() => new Parser().Parse("<%IF a%>x<%ELSE%>"));
            Assert.Equal("a", ex.TokenName);
        }

        [Fact]
        public void ElseTokenHasThePositionOfTheElseTag()
        {
            var token = (ConditionToken)new Parser().Parse("<%IF a%>x<%ELSE%>y<%ENDIF%>")[0];

            var elseToken = Assert.IsType<ElseToken>(token.FalsePart);
            Assert.Equal(1, elseToken.LineNumber);
            Assert.Equal(10, elseToken.StartingPosition);
        }
    }
}
