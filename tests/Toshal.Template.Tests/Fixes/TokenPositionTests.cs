using Toshal.Template.Tokens;
using Xunit;

namespace Toshal.Template.Tests.Fixes
{
    // LineNumber and StartingPosition of every token are 1 based and point to the first character of the token.
    // Before the fix: the first text of a template had 0 and 0, columns after a line break were 0 based,
    // and text after a tag pointed to the "%>" of that tag.
    public class TokenPositionTests
    {
        private static IToken TokenAt(string template, int index) => new Parser().Parse(template)[index];

        [Fact]
        public void TextAtTheStartIsLine1Column1()
        {
            var token = TokenAt("abc", 0);
            Assert.Equal(1, token.LineNumber);
            Assert.Equal(1, token.StartingPosition);
        }

        [Fact]
        public void TextAfterATagStartsAfterTheTag()
        {
            var token = TokenAt("<%=A%>xyz", 1);
            Assert.Equal(1, token.LineNumber);
            Assert.Equal(7, token.StartingPosition);
        }

        [Fact]
        public void TagAtTheStartOfTheSecondLineIsColumn1()
        {
            var token = TokenAt("x\n<%=A%>", 1);
            Assert.Equal(2, token.LineNumber);
            Assert.Equal(1, token.StartingPosition);
        }

        [Fact]
        public void TagInTheMiddleOfTheSecondLine()
        {
            var token = TokenAt("ab\ncd<%=A%>", 1);
            Assert.Equal(2, token.LineNumber);
            Assert.Equal(3, token.StartingPosition);
        }
    }
}
