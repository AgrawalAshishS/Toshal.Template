using System.Collections.Generic;

using Toshal.Template.Tokens;
using Xunit;

namespace Toshal.Template.Tests.Fixes
{
    // The constructors that make child args from parent args, for example new TokenArgs(parentArgs, childContext),
    // must not change the ParentContext list of the parent args. The processor owns that list and uses it as a stack.
    // Before the fix they added the parent context to the same list.
    public class ArgsCopyTests
    {
        [Fact]
        public void TokenArgsCopyLeavesTheParentListAlone()
        {
            var parents = new List<object?> { "root" };
            var parent = new TokenArgs(new NamedToken(new Split { Content = "<%=n%>" }), "ctx", parents);

            var child = new TokenArgs(parent, "child");

            Assert.Equal(new object?[] { "root" }, parents);
            Assert.Equal(new object?[] { "root", "ctx" }, child.ParentContext);
            Assert.Equal("child", child.Context);
        }

        [Fact]
        public void ConditionArgsCopyLeavesTheParentListAlone()
        {
            var parents = new List<object?> { "root" };
            var parent = new ConditionArgs(new ConditionToken(new Split { Content = "<%IF c%>" }), "ctx", parents);

            var child = new ConditionArgs(parent, "child");

            Assert.Equal(new object?[] { "root" }, parents);
            Assert.Equal(new object?[] { "root", "ctx" }, child.ParentContext);
        }

        [Fact]
        public void LoopArgsCopyLeavesTheParentListAlone()
        {
            var parents = new List<object?> { "root" };
            var parent = new LoopArgs(new ForEachToken(new Split { Content = "<%FOREACH l%>" }), "ctx", parents);

            var child = new LoopArgs(parent, "child");

            Assert.Equal(new object?[] { "root" }, parents);
            Assert.Equal(new object?[] { "root", "ctx" }, child.ParentContext);
        }
    }
}
