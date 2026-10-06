using Toshal.Template.Tools;
using Xunit;

namespace Toshal.Template.Tests
{
    // The website in docs/ is generated from the XML comments and the examples. This test fails when someone changed them
    // and forgot to run docs.cmd, so the published site cannot go stale.
    public class DocsTests
    {
        [Fact]
        public void DocsAreUpToDate()
        {
            var differences = SiteBuilder.FindDifferences(RepoRoot.Find());

            Assert.True(differences.Count == 0, "Run docs.cmd and commit docs/. Different files: " + string.Join(", ", differences));
        }
    }
}
