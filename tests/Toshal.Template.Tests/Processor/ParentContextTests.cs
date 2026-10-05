using NUnit.Framework;
using NUnit.Framework.Legacy;
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
                ClassicAssert.AreEqual(0, args.ParentContext.Count);
                called = true;
                return "abc";
            };
            StringBuilder result = processor.Process(new ProcessorArgs(tokens));

            ClassicAssert.AreEqual("abc", result.ToString());
            ClassicAssert.IsTrue(called);
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
                ClassicAssert.AreEqual(1, args.ParentContext.Count);
                called = true;
                return "abc";
            };

            var processArgs = new ProcessorArgs(tokens);
            processArgs.Context = "yes";
            StringBuilder result = processor.Process(processArgs);

            ClassicAssert.AreEqual("abc", result.ToString());
            ClassicAssert.IsTrue(called);
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
                ClassicAssert.AreEqual(1, args.ParentContext.Count);
                calledLoop = true;
                return new List<int>() { 1, 2 };
            };
            processor.TokenValueProvider = (args) => {
                ClassicAssert.AreEqual(3, args.ParentContext.Count);
                calledToken = true;
                return "abc";
            };

            var processArgs = new ProcessorArgs(tokens);
            processArgs.Context = "yes";
            StringBuilder result = processor.Process(processArgs);

            ClassicAssert.AreEqual("abcabc", result.ToString());
            ClassicAssert.IsTrue(calledToken);
            ClassicAssert.IsTrue(calledLoop);
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
                ClassicAssert.AreEqual(1, args.ParentContext.Count);
                calledWith = true;
                return "xyz";
            };
            processor.TokenValueProvider = (args) => {
                ClassicAssert.AreEqual(2, args.ParentContext.Count);
                calledToken = true;
                return "abc";
            };

            var processArgs = new ProcessorArgs(tokens);
            processArgs.Context = "yes";
            StringBuilder result = processor.Process(processArgs);

            ClassicAssert.AreEqual("abc", result.ToString());
            ClassicAssert.IsTrue(calledToken);
            ClassicAssert.IsTrue(calledWith);
        }
    }
}
