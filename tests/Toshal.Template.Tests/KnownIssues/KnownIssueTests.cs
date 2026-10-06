using System;
using System.Collections.Generic;

using Toshal.Template.Exceptions;
using Toshal.Template.Tokens;
using Xunit;

namespace Toshal.Template.Tests.KnownIssues
{
    // Each skipped test shows the wanted behavior of a known issue in docs/known-issues.md.
    // Remove the Skip when the issue is fixed. The other tests pin today's behavior of an open question.
    public class KnownIssueTests
    {
        // By design (owner decision): a list with one row uses the LAST parts; the last row template wins.
        [Fact]
        public void OneRowUsesTheLastRowPart()
        {
            var processor = new Processor { LoopValueProvider = args => new List<int> { 1 } };
            var tokens = new Parser().Parse("<%FOREACH a%><%FIRSTROW%>F<%ENDFIRSTROW%><%LASTROW%>L<%ENDLASTROW%><%ROW%>R<%ENDROW%><%ENDFOR%>");

            Assert.Equal("L", processor.Process(new ProcessorArgs(tokens)).ToString());
        }
    }
}
