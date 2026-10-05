using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace Toshal.Template.Tests.Fixes
{
    // ParentContext is a stack: a block adds its context when it starts and removes that same entry when it ends.
    // Before the fix the processor removed the first equal value, so an inner loop item equal to an outer loop item
    // removed the outer one, and the next inner row saw a wrong ParentContext.
    public class ParentContextStackTests
    {
        [Fact]
        public void InnerItemEqualToOuterItemKeepsTheStackInOrder()
        {
            var processor = new Processor
            {
                LoopValueProvider = args => args.Name == "outer" ? new List<string> { "x" } : new List<string> { "x", "y" },
                TokenValueProvider = args => string.Join(",", args.ParentContext.Select(p => p is IList ? "L" : p)),
            };

            var tokens = new Parser().Parse("<%FOREACH outer%><%FOREACH inner%>[<%=t%>]<%ENDFOR%><%ENDFOR%>");
            string result = processor.Process(new ProcessorArgs(tokens)).ToString();

            Assert.Equal("[L,x,L,x][L,x,L,y]", result);
        }
    }
}
