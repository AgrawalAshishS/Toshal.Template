using System.Collections.Generic;

using Xunit;

namespace Toshal.Template.Tests
{
    // Pins where a SET variable is visible, for every kind of block. The processor shares the variables of a scope
    // until a SET writes, so these tests guard that sharing never lets a variable leak or get lost.
    public class SetScopeTests
    {
        private static string Run(string template)
        {
            var processor = new Processor
            {
                ConditionValueProvider = args => args.Name == "yes",
                LoopValueProvider = args => args.Name == "none" ? new List<int>() : new List<int> { 1, 2, 3 },
                WithValueProvider = args => "with",
                ProcessTemplateValueProvider = args => new Parser().Parse("<%SET v%>sub<%ENDSET%>(<%=v%>)"),
            };

            return processor.Process(new ProcessorArgs(new Parser().Parse(template))).ToString();
        }

        [Fact]
        public void SetInsideIfIsVisibleAfterTheIf()
        {
            Assert.Equal("[if]", Run("<%SET v%>top<%ENDSET%><%IF yes%><%SET v%>if<%ENDSET%><%ENDIF%>[<%=v%>]"));
        }

        [Fact]
        public void SetInsideElseIsVisibleAfterTheIf()
        {
            Assert.Equal("[else]", Run("<%IF no%><%ELSE%><%SET v%>else<%ENDSET%><%ENDIF%>[<%=v%>]"));
        }

        [Fact]
        public void SetInsideIfInsideWithStaysInTheWith()
        {
            Assert.Equal("(if)[top]", Run("<%SET v%>top<%ENDSET%><%WITH w%><%IF yes%><%SET v%>if<%ENDSET%><%ENDIF%>(<%=v%>)<%ENDWITH%>[<%=v%>]"));
        }

        [Fact]
        public void SetInsideWithDoesNotLeak()
        {
            Assert.Equal("(with)[top]", Run("<%SET v%>top<%ENDSET%><%WITH w%><%SET v%>with<%ENDSET%>(<%=v%>)<%ENDWITH%>[<%=v%>]"));
        }

        [Fact]
        public void WithSeesOuterVariables()
        {
            Assert.Equal("(top)", Run("<%SET v%>top<%ENDSET%><%WITH w%>(<%=v%>)<%ENDWITH%>"));
        }

        [Fact]
        public void TwoWithBlocksDoNotShareVariables()
        {
            Assert.Equal("(a)()", Run("<%WITH w%><%SET v%>a<%ENDSET%>(<%=v%>)<%ENDWITH%><%WITH w%>(<%=v%>)<%ENDWITH%>"));
        }

        [Fact]
        public void SetInsideProcessTemplateDoesNotLeak()
        {
            Assert.Equal("(sub)[top]", Run("<%SET v%>top<%ENDSET%><%PROCESS_TEMPLATE s%>[<%=v%>]"));
        }

        [Fact]
        public void SetInsideHeaderDoesNotReachTheRows()
        {
            Assert.Equal("[top][top][top]", Run("<%SET v%>top<%ENDSET%><%FOREACH l%><%HEADER%><%SET v%>h<%ENDSET%><%ENDHEADER%><%ROW%>[<%=v%>]<%ENDROW%><%ENDFOR%>"));
        }

        [Fact]
        public void SetInsideFooterAndNoRecordDoNotLeak()
        {
            Assert.Equal("[top]", Run("<%SET v%>top<%ENDSET%><%FOREACH l%><%ROW%><%ENDROW%><%FOOTER%><%SET v%>f<%ENDSET%><%ENDFOOTER%><%ENDFOR%>"
                + "<%FOREACH none%><%NORECORD%><%SET v%>n<%ENDSET%><%ENDNORECORD%><%ENDFOR%>[<%=v%>]"));
        }

        [Fact]
        public void SetInsideARowIsSeenByTheNextRowButNotAfterTheLoop()
        {
            Assert.Equal("(top)(1)(1)[top]", Run("<%SET v%>top<%ENDSET%><%FOREACH l%><%ROW%>(<%=v%>)<%SET v%>1<%ENDSET%><%ENDROW%><%ENDFOR%>[<%=v%>]"));
        }

        [Fact]
        public void SetInsideBeforeRowIsNotSeenByTheRow()
        {
            Assert.Equal("(top)(top)(top)", Run("<%SET v%>top<%ENDSET%><%FOREACH l%><%BEFOREROW%><%SET v%>b<%ENDSET%><%ENDBEFOREROW%><%ROW%>(<%=v%>)<%ENDROW%><%ENDFOR%>"));
        }

        [Fact]
        public void TwoLoopsDoNotShareRowVariables()
        {
            Assert.Equal("()(1)(1)()(2)(2)", Run("<%FOREACH l%><%ROW%>(<%=v%>)<%SET v%>1<%ENDSET%><%ENDROW%><%ENDFOR%><%FOREACH m%><%ROW%>(<%=v%>)<%SET v%>2<%ENDSET%><%ENDROW%><%ENDFOR%>"));
        }

        [Fact]
        public void SetInsideAnIfInsideARowIsSeenByTheNextRow()
        {
            Assert.Equal("()(1)(1)", Run("<%FOREACH l%><%ROW%>(<%=v%>)<%IF yes%><%SET v%>1<%ENDSET%><%ENDIF%><%ENDROW%><%ENDFOR%>"));
        }

        [Fact]
        public void SetValueSeesItsOwnScope()
        {
            Assert.Equal("[a-b]", Run("<%SET a%>a<%ENDSET%><%SET b%><%=a%>-b<%ENDSET%>[<%=b%>]"));
        }

        [Fact]
        public void ProcessingTwiceStartsWithNoVariables()
        {
            var processor = new Processor();
            var tokens = new Parser().Parse("[<%=v%>]<%SET v%>x<%ENDSET%>");

            Assert.Equal("[]", processor.Process(new ProcessorArgs(tokens)).ToString());
            Assert.Equal("[]", processor.Process(new ProcessorArgs(tokens)).ToString());
        }
    }
}
