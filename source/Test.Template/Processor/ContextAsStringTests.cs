using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Test.Template
{
    using NUnit.Framework.Legacy;
    using System.Text;
    using Toshal.Template;
    using Toshal.Template.Exceptions;
    using Toshal.Template.Tokens;

    [TestFixture]
    public partial class TemplateProcessorTests
    {
        //string value
        //object value
        //null value / no context

        [Test]
        public void ContextAsStringWithNoContextReturnEmpty()
        {
            const string templateText = "<%CONTEXT_AS_STRING%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            ClassicAssert.AreEqual("", result.ToString());
        }

        [Test]
        public void ContextAsStringWithNullContextReturnEmpty()
        {
            const string templateText = "<%CONTEXT_AS_STRING%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            var process = new ProcessorArgs(tokens);
            process.Context = null;
            StringBuilder result = processor.Process(process);
            ClassicAssert.AreEqual("", result.ToString());
        }

        [Test]
        public void ContextAsStringWithStringContextReturnStringAsIs()
        {
            const string templateText = "<%CONTEXT_AS_STRING%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            var process = new ProcessorArgs(tokens);
            process.Context = "testing";
            StringBuilder result = processor.Process(process);
            ClassicAssert.AreEqual("testing", result.ToString());
        }

        [Test]
        public void ContextAsStringWithObjectContextReturnObjectToStringResult()
        {
            const string templateText = "<%CONTEXT_AS_STRING%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            var process = new ProcessorArgs(tokens);
            process.Context = parser;
            StringBuilder result = processor.Process(process);
            ClassicAssert.AreEqual("Toshal.Template.Parser", result.ToString());
        }
    }
}