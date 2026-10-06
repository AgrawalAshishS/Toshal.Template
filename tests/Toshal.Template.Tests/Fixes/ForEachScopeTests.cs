using System.Collections.Generic;

using Toshal.Template.Exceptions;
using Toshal.Template.Tokens;
using Xunit;

namespace Toshal.Template.Tests.Fixes
{
    // Owner decision: a FOREACH name is unique per level. A level is one list of tokens: the top of the template, or the inside of
    // an IF, ELSEIF, ELSE, WITH, SET or FOREACH part. The same name may be used again at a deeper level.
    // REUSE_FOREACH uses the nearest FOREACH: its own level first, then each outer level up to the top.
    // Before: names had to be unique in the whole template only when a REUSE_FOREACH used them, and that failed with ArgumentException.
    public class ForEachScopeTests
    {
        private static List<IToken> Parse(string template) => new Parser().Parse(template);

        [Fact]
        public void SameNameTwiceAtTheSameLevelIsAParserError()
        {
            var ex = Assert.Throws<ParserException>(() => Parse("<%FOREACH a%>1<%ENDFOR%>\n<%FOREACH a%>2<%ENDFOR%>"));
            Assert.Equal(2, ex.LineNumber);
            Assert.Equal(1, ex.StartingPosition);
        }

        [Fact]
        public void SameNameTwiceInsideOneBlockIsAParserError()
        {
            Assert.Throws<ParserException>(() => Parse("<%WITH w%><%FOREACH a%>1<%ENDFOR%><%FOREACH a%>2<%ENDFOR%><%ENDWITH%>"));
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
        public void ProcessingUsesTheNearestLayout()
        {
            var processor = new Processor { LoopValueProvider = args => new[] { 1 }, WithValueProvider = args => "w" };
            var tokens = Parse("<%FOREACH a%>T<%ENDFOR%><%WITH w%><%FOREACH a%>W<%ENDFOR%><%REUSE_FOREACH a b%><%ENDWITH%><%REUSE_FOREACH a c%>");

            Assert.Equal("TWWT", processor.Process(new ProcessorArgs(tokens)).ToString());
        }
    }
}
