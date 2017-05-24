using System.Collections.Generic;
using System.Text;

using NUnit.Framework;
using Test.Template.SupportClass;

namespace Test.Template
{
    using Toshal.Template;
    using Toshal.Template.Tokens;

    [TestFixture]
    public partial class SetProcessorTests
    {
        [Test]
        public void ItShouldRecognizeSet()
        {
            const string templateText = "<%SET MyTokenName %>MyValue<%ENDSET%><%=MyTokenName%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.AreEqual("MyValue", result.ToString());
        }

        [Test]
        public void EmptyValueShouldNotThrowError()
        {
            const string templateText = "<%SET MyTokenName %><%ENDSET%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.AreEqual("", result.ToString());
        }

        [Test]
        public void SetValueAppearInForEach()
        {
            const string templateText = "<%SET MyTokenName %>MyValue<%ENDSET%><%FOREACH A%><%=MyTokenName%><%ENDFOR%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (args) => new List<int>() { 1 };

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.AreEqual("MyValue", result.ToString());
        }

        [Test]
        public void SetValueChangeForContextOnlyInForEach()
        {
            const string templateText = "<%SET MyTokenName %>Out<%ENDSET%><%=MyTokenName%> <%FOREACH A%><%=MyTokenName%> <%SET MyTokenName %>In<%ENDSET%> <%=MyTokenName%><%ENDFOR%> <%=MyTokenName%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.LoopValueProvider = (args) => new List<int>() { 1 };

            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.AreEqual("Out Out  In Out", result.ToString());
        }

        [Test]
        public void SetProcessTokens()
        {
            const string templateText = "<%SET MyTokenName %>MyValue<%ENDSET%><%SET OtherToken %><%=MyTokenName%>Abc<%ENDSET%><%=OtherToken%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            var process = new ProcessorArgs(tokens);

            StringBuilder result = processor.Process(process);
            Assert.AreEqual("MyValueAbc", result.ToString());
        }
    }
}