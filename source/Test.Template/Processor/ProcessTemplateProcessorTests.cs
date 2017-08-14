using System.Collections.Generic;
using System.Text;

using NUnit.Framework;
using Test.Template.SupportClass;

namespace Test.Template
{
    using Toshal.Template;
    using Toshal.Template.Tokens;

    [TestFixture]
    public partial class ProcessTemplateProcessorTests
    {
        [Test]
        public void ItShouldBeAbleToProcessSubTemplate()
        {
            const string mainTemplateText = "This is <%PROCESS_TEMPLATE Sample context=\"abc\"%>";
            const string subTemplateText = "sample.";

            var parser = new Parser();
            List<IToken> tokens = parser.Parse(mainTemplateText);

            Assert.AreEqual(2, tokens.Count);
            Assert.IsInstanceOf<ContentToken>(tokens[0]);
            Assert.IsInstanceOf<ProcessTemplateToken>(tokens[1]);
            Assert.AreEqual("This is ", ((ContentToken)tokens[0]).Content);
            Assert.AreEqual("sample", ((ProcessTemplateToken)tokens[1]).Name);
            Assert.AreEqual("abc", ((ProcessTemplateToken)tokens[1]).GetAttribute("context", "xyz"));

            var processor = new Processor();
            processor.ProcessTemplateValueProvider = (args) => {
                return parser.Parse(subTemplateText);
            };
            StringBuilder result = processor.Process(new ProcessorArgs(tokens));

            Assert.AreEqual("This is sample.", result.ToString());
        }
        
    }
}