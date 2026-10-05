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
    public partial class TemplateProcessorTests
    {
        [Test]
        public void BasicTextContentOnlyTemplate()
        {
            string templateText = "basic text";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);
            var processor = new Processor();
            StringBuilder result = processor.Process(new ProcessorArgs(tokens));
            ClassicAssert.AreEqual(templateText, result.ToString());
        }

        [Test]
        public void BasicNamedTokenTemplate()
        {
            string templateText = "<%=Token%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);
            var processor = new Processor();
            StringBuilder result = processor.Process(new ProcessorArgs(tokens));
            ClassicAssert.AreEqual("", result.ToString());
        }

        [Test]
        public void BasicNamedTokenTemplateWithValueReplacement()
        {
            string templateText = "<%=Token%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.TokenValueProvider = (TokenArgs arg) => "some";

            StringBuilder result = processor.Process(new ProcessorArgs(tokens));
            ClassicAssert.AreEqual("some", result.ToString());
        }

        [Test]
        public void BasicNamedTokenTemplateWithAttributes()
        {
            string templateText = "<%=Token attr=\"Name\"%>";
            var parser = new Parser();
            List<IToken> tokens = parser.Parse(templateText);

            var processor = new Processor();
            processor.TokenValueProvider = (TokenArgs args) => args.Attributes["attr"];

            StringBuilder result = processor.Process(new ProcessorArgs(tokens));
            ClassicAssert.AreEqual("Name", result.ToString());
        }

        [Test]
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
            ClassicAssert.AreEqual("First Value, Second Value", result.ToString());
        }
        
    }
}