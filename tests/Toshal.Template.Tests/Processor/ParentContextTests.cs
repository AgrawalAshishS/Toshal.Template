using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toshal.Template;
using Toshal.Template.Tokens;

namespace Toshal.Template.Tests
{
    public class ParentContextTests
    {
        [Fact]
        public void NoProcessorLevelContext()
        {
            string templateText = "<%=simple%>";
            var called = false;
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.TokenValueProvider = (args) => {
                Assert.Empty(args.ParentContext);
                called = true;
                return "abc";
            };
            StringBuilder result = processor.Process(new ProcessorArgs(tokens));

            Assert.Equal("abc", result.ToString());
            Assert.True(called);
        }

        [Fact]
        public void WithProcessorLevelContext()
        {
            string templateText = "<%=simple%>";
            var called = false;
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.TokenValueProvider = (args) => {
                Assert.Single(args.ParentContext);
                called = true;
                return "abc";
            };

            var processArgs = new ProcessorArgs(tokens);
            processArgs.Context = "yes";
            StringBuilder result = processor.Process(processArgs);

            Assert.Equal("abc", result.ToString());
            Assert.True(called);
        }

        [Fact]
        public void ProcessorLevelContextInForLoop()
        {
            string templateText = "<%FOREACH abc%><%=simple%><%ENDFOR%>";
            bool calledToken = false, calledLoop = false;
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (args) =>
            {
                Assert.Single(args.ParentContext);
                calledLoop = true;
                return new List<int>() { 1, 2 };
            };
            processor.TokenValueProvider = (args) => {
                Assert.Equal(3, args.ParentContext.Count);
                calledToken = true;
                return "abc";
            };

            var processArgs = new ProcessorArgs(tokens);
            processArgs.Context = "yes";
            StringBuilder result = processor.Process(processArgs);

            Assert.Equal("abcabc", result.ToString());
            Assert.True(calledToken);
            Assert.True(calledLoop);
        }

        [Fact]
        public void ProcessorLevelContextInWithToken()
        {
            string templateText = "<%WITH abc%><%=simple%><%ENDWITH%>";
            bool calledToken = false, calledWith = false;
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.WithValueProvider = (args) =>
            {
                Assert.Single(args.ParentContext);
                calledWith = true;
                return "xyz";
            };
            processor.TokenValueProvider = (args) => {
                Assert.Equal(2, args.ParentContext.Count);
                calledToken = true;
                return "abc";
            };

            var processArgs = new ProcessorArgs(tokens);
            processArgs.Context = "yes";
            StringBuilder result = processor.Process(processArgs);

            Assert.Equal("abc", result.ToString());
            Assert.True(calledToken);
            Assert.True(calledWith);
        }
    }
}
