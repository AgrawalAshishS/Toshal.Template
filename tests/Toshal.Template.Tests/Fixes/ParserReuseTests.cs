using System;

using Toshal.Template.Exceptions;
using Toshal.Template.Tokens;
using Xunit;

namespace Toshal.Template.Tests.Fixes
{
    // One Parser instance must parse many templates, and each template must only see its own FOREACH blocks.
    // Before the fix the parser kept the FOREACH blocks of earlier templates.
    public class ParserReuseTests
    {
        [Fact]
        public void SameParserParsesTwoTemplatesWithReuse()
        {
            var parser = new Parser();
            parser.Parse("<%FOREACH a%>x<%ENDFOR%><%REUSE_FOREACH a b%>");

            var tokens = parser.Parse("<%FOREACH a%>y<%ENDFOR%><%REUSE_FOREACH a b%>");

            var reuse = Assert.IsType<ReuseForEachToken>(tokens[1]);
            Assert.Same(tokens[0], reuse.ExistingForEachToken);
        }

        [Fact]
        public void ReuseDoesNotFindAForEachOfAnEarlierTemplate()
        {
            var parser = new Parser();
            parser.Parse("<%FOREACH a%>x<%ENDFOR%>");

            Assert.Throws<ForEachMissingForReuseException>(() => parser.Parse("<%REUSE_FOREACH a b%>"));
        }

        // Two FOREACH blocks with the same name are fine at different levels, and REUSE_FOREACH of another name still works.
        // (Owner decision: the same name twice at one level is a ParserException; see ForEachScopeTests.)
        [Fact]
        public void DuplicateForEachNamesAtDifferentLevelsDoNotBreakReuseOfAnotherName()
        {
            var tokens = new Parser().Parse("<%FOREACH a%><%ENDFOR%><%IF x%><%FOREACH a%><%ENDFOR%><%ENDIF%><%FOREACH b%>x<%ENDFOR%><%REUSE_FOREACH b c%>");

            var reuse = Assert.IsType<ReuseForEachToken>(tokens[3]);
            Assert.Same(tokens[2], reuse.ExistingForEachToken);
        }
    }
}
