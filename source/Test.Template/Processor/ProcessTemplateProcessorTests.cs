using System.Collections.Generic;
using System.Text;

using NUnit.Framework;
using Test.Template.SupportClass;

namespace Test.Template
{
    using NUnit.Framework.Legacy;
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

            ClassicAssert.AreEqual(2, tokens.Count);
            ClassicAssert.IsInstanceOf<ContentToken>(tokens[0]);
            ClassicAssert.IsInstanceOf<ProcessTemplateToken>(tokens[1]);
            ClassicAssert.AreEqual("This is ", ((ContentToken)tokens[0]).Content);
            ClassicAssert.AreEqual("sample", ((ProcessTemplateToken)tokens[1]).Name);
            ClassicAssert.AreEqual("abc", ((ProcessTemplateToken)tokens[1]).GetAttribute("context", "xyz"));

            var processor = new Processor();
            processor.ProcessTemplateValueProvider = (args) => {
                return parser.Parse(subTemplateText);
            };
            StringBuilder result = processor.Process(new ProcessorArgs(tokens));

            ClassicAssert.AreEqual("This is sample.", result.ToString());
        }
        
    }
}