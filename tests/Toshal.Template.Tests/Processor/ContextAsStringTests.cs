using System;
using System.Collections.Generic;

using Xunit;

namespace Toshal.Template.Tests
{
    using System.Text;
    using Toshal.Template;
    using Toshal.Template.Exceptions;
    using Toshal.Template.Tokens;

    public partial class TemplateProcessorTests
    {
        //string value
        //object value
        //null value / no context

        [Fact]
        public void ContextAsStringWithNoContextReturnEmpty()
        {
            const string templateText = "<%CONTEXT_AS_STRING%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            var process = new ProcessorArgs(tokens);
            StringBuilder result = processor.Process(process);
            Assert.Equal("", result.ToString());
        }

        [Fact]
        public void ContextAsStringWithNullContextReturnEmpty()
        {
            const string templateText = "<%CONTEXT_AS_STRING%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            var process = new ProcessorArgs(tokens);
            process.Context = null;
            StringBuilder result = processor.Process(process);
            Assert.Equal("", result.ToString());
        }

        [Fact]
        public void ContextAsStringWithStringContextReturnStringAsIs()
        {
            const string templateText = "<%CONTEXT_AS_STRING%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            var process = new ProcessorArgs(tokens);
            process.Context = "testing";
            StringBuilder result = processor.Process(process);
            Assert.Equal("testing", result.ToString());
        }

        [Fact]
        public void ContextAsStringWithObjectContextReturnObjectToStringResult()
        {
            const string templateText = "<%CONTEXT_AS_STRING%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            var process = new ProcessorArgs(tokens);
            process.Context = parser;
            StringBuilder result = processor.Process(process);
            Assert.Equal("Toshal.Template.Parser", result.ToString());
        }
    }
}