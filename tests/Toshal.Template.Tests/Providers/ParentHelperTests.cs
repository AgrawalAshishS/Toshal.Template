using System;
using System.Collections.Generic;

using Toshal.Template.Tokens;
using Xunit;

namespace Toshal.Template.Tests.Providers
{
    public class ParentHelperTests
    {
        private sealed class Order { }

        private sealed class Line { }

        private static TokenArgs Args(params object?[] parents)
        {
            var token = (NamedToken)new Parser().Parse("<%=x%>")[0];
            return new TokenArgs(token, parents.Length > 0 ? parents[^1] : null, new List<object?>(parents));
        }

        [Fact]
        public void FindParentReturnsTheNearestFromTheInside()
        {
            var outer = new Order();
            var inner = new Order();
            var args = Args(outer, inner, new Line());

            Assert.Same(inner, args.FindParent<Order>());
        }

        [Fact]
        public void FindParentIncludesTheCurrentContext()
        {
            var line = new Line();

            Assert.Same(line, Args(new Order(), line).FindParent<Line>());
        }

        [Fact]
        public void FindParentReturnsNullWhenNoneMatches()
        {
            Assert.Null(Args(new Line(), null).FindParent<Order>());
        }

        [Fact]
        public void ParentCountsLevelsUpFromTheLastEntry()
        {
            var order = new Order();
            var lines = new List<Line>();
            var line = new Line();
            var args = Args(order, lines, line);

            Assert.Same(line, args.Parent(0));
            Assert.Same(lines, args.Parent(1));
            Assert.Same(order, args.Parent(2));
        }

        [Fact]
        public void ParentReturnsNullAboveTheTop()
        {
            Assert.Null(Args(new Order()).Parent(1));
            Assert.Null(Args().Parent(0));
        }

        [Fact]
        public void ParentThrowsForANegativeLevel()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Args(new Order()).Parent(-1));
        }

        [Fact]
        public void HelpersReadTheLiveStackOfTheProcessor()
        {
            var order = new Order();
            var line = new Line();
            object? found = null;
            object? parent = null;
            var processor = new Processor
            {
                LoopValueProvider = args => new List<Line> { line },
                TokenValueProvider = args =>
                {
                    found = args.FindParent<Order>();
                    parent = args.Parent(1);
                    return null;
                },
            };

            processor.Process(new ProcessorArgs(new Parser().Parse("<%FOREACH lines%><%=x%><%ENDFOR%>")) { Context = order });

            Assert.Same(order, found);
            Assert.IsType<List<Line>>(parent);
        }
    }
}
