using System.Collections.Generic;

using Toshal.Template.Exceptions;
using Toshal.Template.Tokens;
using Xunit;

namespace Toshal.Template.Tests.Fixes
{
    // Owner decision: a FOREACH name is the name of its list, so the same name may be used as often as wanted, at any level.
    // REUSE_FOREACH finds its FOREACH in this order:
    // 1. the FOREACH whose id attribute is the first word (ids are unique in the whole template),
    // 2. the nearest FOREACH with that name above the REUSE_FOREACH: its own level first, then each outer level up to the top,
    // 3. the first FOREACH with that name in the whole template, top to bottom.
    // Before: names had to be unique in the whole template only when a REUSE_FOREACH used them, and that failed with ArgumentException.
    // Later the name was unique per level, which was wrong: a list can be shown more than once.
    public class ForEachScopeTests
    {
        private static List<IToken> Parse(string template) => new Parser().Parse(template);

        [Fact]
        public void SameNameTwiceAtTheSameLevelIsAllowed()
        {
            Assert.Equal(2, Parse("<%FOREACH a%>1<%ENDFOR%><%FOREACH a%>2<%ENDFOR%>").Count);
        }

        [Fact]
        public void SameNameTwiceInsideOneBlockIsAllowed()
        {
            var with = (WithToken)Parse("<%WITH w%><%FOREACH a%>1<%ENDFOR%><%FOREACH a%>2<%ENDFOR%><%ENDWITH%>")[0];
            Assert.Equal(2, with.InnerTokens.Count);
        }

        [Fact]
        public void SameNameAtADeeperLevelIsAllowed()
        {
            var tokens = Parse("<%FOREACH a%><%FOREACH a%>inner<%ENDFOR%><%ENDFOR%><%IF x%><%FOREACH a%>if<%ENDFOR%><%ENDIF%>");
            Assert.Equal(2, tokens.Count);
        }

        [Fact]
        public void SameNameInIfAndElseIsAllowed()
        {
            var tokens = Parse("<%IF x%><%FOREACH a%>1<%ENDFOR%><%ELSE%><%FOREACH a%>2<%ENDFOR%><%ENDIF%>");
            Assert.Single(tokens);
        }

        [Fact]
        public void ReuseUsesTheNearestForEach()
        {
            var tokens = Parse("<%FOREACH a%>T<%ENDFOR%><%WITH w%><%FOREACH a%>W<%ENDFOR%><%REUSE_FOREACH a b%><%ENDWITH%><%REUSE_FOREACH a c%>");

            var top = (ForEachToken)tokens[0];
            var with = (WithToken)tokens[1];
            var inner = (ForEachToken)with.InnerTokens[0];
            Assert.Same(inner, ((ReuseForEachToken)with.InnerTokens[1]).ExistingForEachToken);
            Assert.Same(top, ((ReuseForEachToken)tokens[2]).ExistingForEachToken);
        }

        [Fact]
        public void ReuseFindsAForEachAtAnOuterLevel()
        {
            var tokens = Parse("<%FOREACH a%>T<%ENDFOR%><%IF x%><%REUSE_FOREACH a b%><%ENDIF%>");

            var reuse = (ReuseForEachToken)((ConditionToken)tokens[1]).InnerTokens[0];
            Assert.Same(tokens[0], reuse.ExistingForEachToken);
        }

        // Fallback (owner decision): when no level from the REUSE_FOREACH outwards has the name, the first FOREACH with that name
        // in the whole template (top to bottom) is used.
        [Fact]
        public void ReuseFallsBackToAForEachInsideAnotherBlock()
        {
            var tokens = Parse("<%IF x%><%FOREACH a%>T<%ENDFOR%><%ENDIF%><%REUSE_FOREACH a b%>");

            var inIf = ((ConditionToken)tokens[0]).InnerTokens[0];
            Assert.Same(inIf, ((ReuseForEachToken)tokens[1]).ExistingForEachToken);
        }

        [Fact]
        public void FallbackUsesTheFirstForEachInTemplateOrder()
        {
            // The outer "a" starts first in the text, even though the inner "a" is finished first while parsing.
            var tokens = Parse("<%IF x%><%FOREACH a%>1<%FOREACH a%>2<%ENDFOR%><%ENDFOR%><%ENDIF%><%IF y%><%FOREACH a%>3<%ENDFOR%><%ENDIF%><%WITH w%><%REUSE_FOREACH a b%><%ENDWITH%>");

            var outer = ((ConditionToken)tokens[0]).InnerTokens[0];
            var reuse = (ReuseForEachToken)((WithToken)tokens[2]).InnerTokens[0];
            Assert.Same(outer, reuse.ExistingForEachToken);
        }

        [Fact]
        public void NearestStillWinsOverTheFallback()
        {
            var tokens = Parse("<%IF x%><%FOREACH a%>1<%ENDFOR%><%ENDIF%><%FOREACH a%>2<%ENDFOR%><%REUSE_FOREACH a b%>");

            Assert.Same(tokens[1], ((ReuseForEachToken)tokens[2]).ExistingForEachToken);
        }

        [Fact]
        public void NameNowhereInTheTemplateStillThrows()
        {
            Assert.Throws<ForEachMissingForReuseException>(() => Parse("<%IF x%><%FOREACH a%>T<%ENDFOR%><%ENDIF%><%REUSE_FOREACH z b%>"));
        }

        [Fact]
        public void ReuseUsesTheNearestForEachAboveAtItsLevel()
        {
            var tokens = Parse("<%FOREACH a%>1<%ENDFOR%><%FOREACH a%>2<%ENDFOR%><%REUSE_FOREACH a b%><%FOREACH a%>3<%ENDFOR%>");

            Assert.Same(tokens[1], ((ReuseForEachToken)tokens[2]).ExistingForEachToken);
        }

        [Fact]
        public void ReuseUsesTheNearestForEachAboveAtAnOuterLevel()
        {
            var tokens = Parse("<%FOREACH a%>1<%ENDFOR%><%FOREACH a%>2<%ENDFOR%><%IF x%><%REUSE_FOREACH a b%><%ENDIF%><%FOREACH a%>3<%ENDFOR%>");

            var reuse = (ReuseForEachToken)((ConditionToken)tokens[2]).InnerTokens[0];
            Assert.Same(tokens[1], reuse.ExistingForEachToken);
        }

        [Fact]
        public void AForEachAboveAtAnOuterLevelWinsOverOneBelowAtTheOwnLevel()
        {
            var tokens = Parse("<%FOREACH a%>T<%ENDFOR%><%WITH w%><%REUSE_FOREACH a b%><%FOREACH a%>W<%ENDFOR%><%ENDWITH%>");

            var reuse = (ReuseForEachToken)((WithToken)tokens[1]).InnerTokens[0];
            Assert.Same(tokens[0], reuse.ExistingForEachToken);
        }

        [Fact]
        public void ReuseOfAForEachOnlyBelowFallsBackToTheFirstOne()
        {
            var tokens = Parse("<%REUSE_FOREACH a b%><%FOREACH a%>1<%ENDFOR%><%FOREACH a%>2<%ENDFOR%>");

            Assert.Same(tokens[1], ((ReuseForEachToken)tokens[0]).ExistingForEachToken);
        }

        [Fact]
        public void ReuseFindsAForEachById()
        {
            var tokens = Parse("<%FOREACH a id=\"one\"%>1<%ENDFOR%><%FOREACH a id=\"two\"%>2<%ENDFOR%><%REUSE_FOREACH one b%>");

            Assert.Same(tokens[0], ((ReuseForEachToken)tokens[2]).ExistingForEachToken);
        }

        [Fact]
        public void IdWinsOverAForEachName()
        {
            var tokens = Parse("<%FOREACH x%>1<%ENDFOR%><%FOREACH a id=\"x\"%>2<%ENDFOR%><%REUSE_FOREACH x b%>");

            Assert.Same(tokens[1], ((ReuseForEachToken)tokens[2]).ExistingForEachToken);
        }

        [Fact]
        public void IdIsFoundAnywhereAndIsNotCaseSensitive()
        {
            var tokens = Parse("<%REUSE_FOREACH Main b%><%IF x%><%FOREACH a id=\"MAIN\"%>1<%ENDFOR%><%ENDIF%>");

            var inIf = ((ConditionToken)tokens[1]).InnerTokens[0];
            Assert.Same(inIf, ((ReuseForEachToken)tokens[0]).ExistingForEachToken);
        }

        [Fact]
        public void SameIdTwiceIsAParserError()
        {
            var ex = Assert.Throws<ParserException>(() => Parse("<%FOREACH a id=\"one\"%>1<%ENDFOR%>\n<%IF x%><%FOREACH b id=\"One\"%>2<%ENDFOR%><%ENDIF%>"));
            Assert.Equal(2, ex.LineNumber);
            Assert.Equal(9, ex.StartingPosition);
        }

        [Fact]
        public void ProcessingUsesTheLayoutWithTheId()
        {
            var processor = new Processor { LoopValueProvider = args => new[] { args.Attributes.GetValue("id", "-") } };
            var tokens = Parse("<%FOREACH a id=\"one\"%>1<%CONTEXT_AS_STRING%><%ENDFOR%><%FOREACH a id=\"two\"%>2<%ENDFOR%>|<%REUSE_FOREACH one b%>");

            Assert.Equal("1one2|1one", processor.Process(new ProcessorArgs(tokens)).ToString());
        }

        [Fact]
        public void ProcessingUsesTheNearestLayout()
        {
            var processor = new Processor { LoopValueProvider = args => new[] { 1 }, WithValueProvider = args => "w" };
            var tokens = Parse("<%FOREACH a%>T<%ENDFOR%><%WITH w%><%FOREACH a%>W<%ENDFOR%><%REUSE_FOREACH a b%><%ENDWITH%><%REUSE_FOREACH a c%>");

            Assert.Equal("TWWT", processor.Process(new ProcessorArgs(tokens)).ToString());
        }
    }
}
