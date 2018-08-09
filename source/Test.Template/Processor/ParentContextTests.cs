using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toshal.Template;
using Toshal.Template.Tokens;

namespace Test.Template
{
    [TestFixture]
    public class ParentContextTests
    {
        [Test]
        public void NoProcessorLevelContext()
        {
            string templateText = "<%=simple%>";
            var called = false;
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.TokenValueProvider = (args) => {
                Assert.AreEqual(0, args.ParentContext.Count);
                called = true;
                return "abc";
            };
            StringBuilder result = processor.Process(new ProcessorArgs(tokens));

            Assert.AreEqual("abc", result.ToString());
            Assert.IsTrue(called);
        }

        [Test]
        public void WithProcessorLevelContext()
        {
            string templateText = "<%=simple%>";
            var called = false;
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.TokenValueProvider = (args) => {
                Assert.AreEqual(1, args.ParentContext.Count);
                called = true;
                return "abc";
            };

            var processArgs = new ProcessorArgs(tokens);
            processArgs.Context = "yes";
            StringBuilder result = processor.Process(processArgs);

            Assert.AreEqual("abc", result.ToString());
            Assert.IsTrue(called);
        }

        [Test]
        public void ProcessorLevelContextInForLoop()
        {
            string templateText = "<%FOREACH abc%><%=simple%><%ENDFOR%>";
            bool calledToken = false, calledLoop = false;
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (args) =>
            {
                Assert.AreEqual(1, args.ParentContext.Count);
                calledLoop = true;
                return new List<int>() { 1, 2 };
            };
            processor.TokenValueProvider = (args) => {
                Assert.AreEqual(3, args.ParentContext.Count);
                calledToken = true;
                return "abc";
            };

            var processArgs = new ProcessorArgs(tokens);
            processArgs.Context = "yes";
            StringBuilder result = processor.Process(processArgs);

            Assert.AreEqual("abcabc", result.ToString());
            Assert.IsTrue(calledToken);
            Assert.IsTrue(calledLoop);
        }

        [Test]
        public void ProcessorLevelContextInWithToken()
        {
            string templateText = "<%WITH abc%><%=simple%><%ENDWITH%>";
            bool calledToken = false, calledWith = false;
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.WithValueProvider = (args) =>
            {
                Assert.AreEqual(1, args.ParentContext.Count);
                calledWith = true;
                return "xyz";
            };
            processor.TokenValueProvider = (args) => {
                Assert.AreEqual(2, args.ParentContext.Count);
                calledToken = true;
                return "abc";
            };

            var processArgs = new ProcessorArgs(tokens);
            processArgs.Context = "yes";
            StringBuilder result = processor.Process(processArgs);

            Assert.AreEqual("abc", result.ToString());
            Assert.IsTrue(calledToken);
            Assert.IsTrue(calledWith);
        }
    }
}
