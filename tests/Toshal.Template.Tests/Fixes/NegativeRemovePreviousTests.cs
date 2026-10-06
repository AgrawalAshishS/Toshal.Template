using Toshal.Template.Exceptions;
using Xunit;

namespace Toshal.Template.Tests.Fixes
{
    // A negative count in <%REMOVE_PREVIOUS n%> is rejected by the parser, like a count that is not a whole number.
    // Before the fix the parser accepted it and the processor threw ArgumentOutOfRangeException.
    public class NegativeRemovePreviousTests
    {
        [Fact]
        public void NegativeCountIsAParserError()
        {
            var ex = Assert.Throws<ParserException>(() => new Parser().Parse("ab<%REMOVE_PREVIOUS -1%>"));
            Assert.Equal("<%REMOVE_PREVIOUS -1%>", ex.Split!.Content);
        }

        [Fact]
        public void ZeroRemovesNothing()
        {
            var tokens = new Parser().Parse("ab<%REMOVE_PREVIOUS 0%>");
            Assert.Equal("ab", new Processor().Process(new ProcessorArgs(tokens)).ToString());
        }
    }
}
