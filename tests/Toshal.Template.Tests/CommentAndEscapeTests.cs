using Toshal.Template.Exceptions;
using Toshal.Template.Tokens;

using Xunit;

namespace Toshal.Template.Tests
{
    // <%-- ... --%> is a comment: it writes nothing. \<\% and \%\> write <% and %> as plain text.
    public class CommentAndEscapeTests
    {
        private static string Run(string template)
        {
            var processor = new Processor
            {
                TokenValueProvider = args => args.Name.ToUpperInvariant(),
                ConditionValueProvider = args => args.Name == "yes",
            };
            return processor.Process(new ProcessorArgs(new Parser().Parse(template))).ToString();
        }

        [Fact]
        public void CommentWritesNothing()
        {
            Assert.Equal("a b", Run("a <%-- note --%>b"));
        }

        [Fact]
        public void CommentGivesNoToken()
        {
            var tokens = new Parser().Parse("<%-- note --%>");
            Assert.Empty(tokens);
        }

        [Fact]
        public void EmptyCommentWritesNothing()
        {
            Assert.Equal("ab", Run("a<%----%>b"));
        }

        [Fact]
        public void CommentMayHoldTagsAndCloseTags()
        {
            Assert.Equal("ab", Run("a<%-- <%=x%> <%IF y%> 50%> --%>b"));
        }

        [Fact]
        public void CommentMaySpanLines()
        {
            Assert.Equal("a\r\nb", Run("a\r\n<%-- line one\r\nline two --%>b"));
        }

        [Fact]
        public void LineWithOnlyACommentWritesNothing()
        {
            Assert.Equal("a\nb\n", Run("a\n    <%-- note --%>\nb\n"));
        }

        [Fact]
        public void LineWithACommentAndControlTagsWritesNothing()
        {
            Assert.Equal("a\nin\nb\n", Run("a\n<%IF yes%> <%-- only when yes --%>\nin\n<%ENDIF%>\nb\n"));
        }

        [Fact]
        public void MultiLineCommentAloneWritesNothing()
        {
            Assert.Equal("a\r\nb\r\n", Run("a\r\n  <%-- one\r\n  two --%>\r\nb\r\n"));
        }

        [Fact]
        public void LineWithACommentAndAValueIsKept()
        {
            Assert.Equal("  X\n", Run("  <%=x%><%-- the x --%>\n"));
        }

        [Fact]
        public void CommentInsideABlock()
        {
            Assert.Equal("in", Run("<%IF yes%>i<%-- c --%>n<%ELSE%>out<%ENDIF%>"));
        }

        [Fact]
        public void CommentKeepsTheLineNumbersOfLaterTokens()
        {
            var tokens = new Parser().Parse("<%-- a\nb --%><%=x%>");
            var named = Assert.IsType<NamedToken>(Assert.Single(tokens));
            Assert.Equal(2, named.LineNumber);
            Assert.Equal(7, named.StartingPosition);
        }

        [Fact]
        public void CommentNotClosedThrows()
        {
            var ex = Assert.Throws<TokenNotClosedException>(() => new Parser().Parse("a\n <%-- note %>"));
            Assert.Equal(2, ex.LineNumber);
            Assert.Equal(2, ex.StartingPosition);
        }

        [Fact]
        public void ShortCommentNotClosedThrows()
        {
            Assert.Throws<TokenNotClosedException>(() => new Parser().Parse("<%--%>"));
        }

        [Fact]
        public void EscapedOpenAndCloseWriteTheTagText()
        {
            Assert.Equal("<%=Name%>", Run(@"\<\%=Name\%\>"));
        }

        [Fact]
        public void EscapesNextToRealTags()
        {
            Assert.Equal("<% X %>", Run(@"\<\% <%=x%> \%\>"));
        }

        [Fact]
        public void EscapedTagTextIsOneContentToken()
        {
            var tokens = new Parser().Parse(@"a \<\%IF x\%\> b");
            var content = Assert.IsType<ContentToken>(Assert.Single(tokens));
            Assert.Equal("a <%IF x%> b", content.Content);
        }

        [Fact]
        public void EscapedCloseAloneIsText()
        {
            Assert.Equal("50%>", Run(@"50\%\>"));
        }

        [Fact]
        public void LineWithAnEscapedTagIsKept()
        {
            Assert.Equal("<%IF x%>\n", Run("\\<\\%IF x\\%\\>\n"));
        }

        [Fact]
        public void OtherBackslashesStay()
        {
            Assert.Equal(@"a\b \< \% <%% x", Run(@"a\b \< \% \<\%% x"));
        }
    }
}
