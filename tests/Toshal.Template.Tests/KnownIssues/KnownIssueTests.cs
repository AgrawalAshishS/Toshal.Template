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
        private const string Waiting = "Known issue, see docs/known-issues.md. Waiting for the owner's decision.";

        [Fact(Skip = Waiting)]
        public void IfAttributeValuesKeepTheirCase()
        {
            var token = (ConditionToken)new Parser().Parse("<%IF Weight unit=\"KG\"%>x<%ENDIF%>")[0];
            Assert.Equal("KG", token.Attributes["unit"]);
        }

        [Fact(Skip = Waiting)]
        public void ForEachAttributeValuesKeepTheirCase()
        {
            var token = (ForEachToken)new Parser().Parse("<%FOREACH Lines sort=\"Name\"%>x<%ENDFOR%>")[0];
            Assert.Equal("Name", token.Attributes["sort"]);
        }

        // Today: lower cased. This pins it so a change is noticed.
        [Fact]
        public void IfAndForEachAttributeValuesAreLowerCasedToday()
        {
            var condition = (ConditionToken)new Parser().Parse("<%IF Weight unit=\"KG\"%>x<%ENDIF%>")[0];
            var loop = (ForEachToken)new Parser().Parse("<%FOREACH Lines sort=\"Name\"%>x<%ENDFOR%>")[0];

            Assert.Equal("kg", condition.Attributes["unit"]);
            Assert.Equal("name", loop.Attributes["sort"]);
        }

        // Proposal: reject a negative count when parsing, like a count that is not a whole number.
        [Fact(Skip = Waiting)]
        public void NegativeRemovePreviousIsAParserError()
        {
            Assert.Throws<ParserException>(() => new Parser().Parse("ab<%REMOVE_PREVIOUS -1%>"));
        }

        [Fact]
        public void NegativeRemovePreviousThrowsWhenProcessedToday()
        {
            var tokens = new Parser().Parse("ab<%REMOVE_PREVIOUS -1%>");
            Assert.Throws<ArgumentOutOfRangeException>(() => new Processor().Process(new ProcessorArgs(tokens)));
        }

        // Open question: should a list with one row use FIRSTROW, LASTROW, or both? Today it uses LASTROW.
        [Fact]
        public void OneRowUsesTheLastRowPartToday()
        {
            var processor = new Processor { LoopValueProvider = args => new List<int> { 1 } };
            var tokens = new Parser().Parse("<%FOREACH a%><%FIRSTROW%>F<%ENDFIRSTROW%><%LASTROW%>L<%ENDLASTROW%><%ROW%>R<%ENDROW%><%ENDFOR%>");

            Assert.Equal("L", processor.Process(new ProcessorArgs(tokens)).ToString());
        }
    }
}
