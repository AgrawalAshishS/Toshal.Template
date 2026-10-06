using System.Collections.Generic;
using System.Text;

using Xunit;
using Toshal.Template.Tests.SupportClass;

namespace Toshal.Template.Tests
{
    using Toshal.Template;
    using Toshal.Template.Tokens;

    public partial class TemplateProcessorTests
    {
        [Fact]
        public void BasicTextContentOnlyTemplate()
        {
            string templateText = "basic text";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);
            var processor = new Processor();
            StringBuilder result = processor.Process(new ProcessorArgs(tokens));
            Assert.Equal(templateText, result.ToString());
        }

        [Fact]
        public void BasicNamedTokenTemplate()
        {
            string templateText = "<%=Token%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);
            var processor = new Processor();
            StringBuilder result = processor.Process(new ProcessorArgs(tokens));
            Assert.Equal("", result.ToString());
        }

        [Fact]
        public void BasicNamedTokenTemplateWithValueReplacement()
        {
            string templateText = "<%=Token%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.TokenValueProvider = (TokenArgs arg) => "some";

            StringBuilder result = processor.Process(new ProcessorArgs(tokens));
            Assert.Equal("some", result.ToString());
        }

        [Fact]
        public void BasicNamedTokenTemplateWithAttributes()
        {
            string templateText = "<%=Token attr=\"Name\"%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.TokenValueProvider = (TokenArgs args) => args.Attributes["attr"];

            StringBuilder result = processor.Process(new ProcessorArgs(tokens));
            Assert.Equal("Name", result.ToString());
        }

        [Fact]
        public void BasicNamedTokenTemplateWithMultipleTokens()
        {
            string templateText = "<%=Token1%>, <%=Token2%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.TokenValueProvider = (TokenArgs args) =>
            {
                switch (args.Name)
                {
                    case "token1":
                        return "First Value";
                    case "token2":
                        return "Second Value";
                }
                return null;
            };

            StringBuilder result = processor.Process(new ProcessorArgs(tokens));
            Assert.Equal("First Value, Second Value", result.ToString());
        }

    }
}