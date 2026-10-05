using Xunit;

namespace Toshal.Template.Tests.Fixes
{
    // "%>" without an open tag is plain text and must be written to the output.
    // Before the fix it was lost when no text came right before it (start of template, or right after a tag).
    public class StrayCloseTagTests
    {
        private static string Run(string template)
        {
            var processor = new Processor { TokenValueProvider = args => args.Name.ToUpperInvariant() };
            return processor.Process(new ProcessorArgs(new Parser().Parse(template))).ToString();
        }

        [Fact]
        public void CloseTagAtTheStartIsKept()
        {
            Assert.Equal("%>abc", Run("%>abc"));
        }

        [Fact]
        public void CloseTagRightAfterATagIsKept()
        {
            Assert.Equal("A%>b", Run("<%=a%>%>b"));
        }

        [Fact]
        public void CloseTagAfterTextIsKept()
        {
            Assert.Equal("50%> done", Run("50%> done"));
        }
    }
}
