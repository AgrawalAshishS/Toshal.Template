using System.Collections.Generic;
using System.Text;

using Xunit;
using Toshal.Template.Tests.SupportClass;

namespace Toshal.Template.Tests
{
    using Toshal.Template;
    using Toshal.Template.Tokens;

    public partial class ProcessTemplateProcessorTests
    {
        [Fact]
        public void ItShouldBeAbleToProcessSubTemplate()
        {
            const string mainTemplateText = "This is <%PROCESS_TEMPLATE Sample context=\"abc\"%>";
            const string subTemplateText = "sample.";

            var parser = new Parser();
            List<IToken> tokens = parser.Parse(mainTemplateText);

            Assert.Equal(2, tokens.Count);
            Assert.IsAssignableFrom<ContentToken>(tokens[0]);
            Assert.IsAssignableFrom<ProcessTemplateToken>(tokens[1]);
            Assert.Equal("This is ", ((ContentToken)tokens[0]).Content);
            Assert.Equal("sample", ((ProcessTemplateToken)tokens[1]).Name);
            Assert.Equal("abc", ((ProcessTemplateToken)tokens[1]).GetAttribute("context", "xyz"));

            var processor = new Processor();
            processor.ProcessTemplateValueProvider = (args) =>
            {
                return parser.Parse(subTemplateText);
            };
            StringBuilder result = processor.Process(new ProcessorArgs(tokens));

            Assert.Equal("This is sample.", result.ToString());
        }

    }
}