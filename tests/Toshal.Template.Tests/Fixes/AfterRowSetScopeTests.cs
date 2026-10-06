using System.Collections.Generic;

using Xunit;

namespace Toshal.Template.Tests.Fixes
{
    // AFTERROW must use the row level variables, like BEFOREROW does.
    // Before the fix it used the variables of the block around the loop, so a SET inside AFTERROW changed them.
    public class AfterRowSetScopeTests
    {
        private static string Run(string template)
        {
            var processor = new Processor { LoopValueProvider = args => new List<int> { 1, 2 } };
            return processor.Process(new ProcessorArgs(new Parser().Parse(template))).ToString();
        }

        [Fact]
        public void SetInsideAfterRowDoesNotLeakOutOfTheLoop()
        {
            string template = "<%SET V%>outer<%ENDSET%>"
                + "<%FOREACH L%><%ROW%>r<%ENDROW%><%AFTERROW%><%SET V%>inner<%ENDSET%><%ENDAFTERROW%><%ENDFOR%>"
                + "[<%=V%>]";

            Assert.Equal("rr[outer]", Run(template));
        }

        [Fact]
        public void AfterRowSeesVariablesSetInTheRow()
        {
            string template = "<%FOREACH L%><%ROW%><%SET V%>row<%ENDSET%><%ENDROW%><%AFTERROW%>(<%=V%>)<%ENDAFTERROW%><%ENDFOR%>";

            Assert.Equal("(row)(row)", Run(template));
        }
    }
}
