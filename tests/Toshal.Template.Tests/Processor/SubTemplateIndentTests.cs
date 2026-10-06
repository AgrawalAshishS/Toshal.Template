using System.Collections.Generic;

using Toshal.Template.Tokens;
using Xunit;

namespace Toshal.Template.Tests
{
    // A PROCESS_TEMPLATE alone on its line indents every line of the sub template by the indent of the tag.
    public class SubTemplateIndentTests
    {
        private static string Run(string template, Dictionary<string, string> subTemplates)
        {
            var processor = new Processor
            {
                TokenValueProvider = args => args.Name,
                ConditionValueProvider = args => true,
                ProcessTemplateValueProvider = args => new Parser().Parse(subTemplates[args.Name]),
            };

            return processor.Process(new ProcessorArgs(new Parser().Parse(template))).ToString();
        }

        private const string NullCheck = "if (customer == null)\n{\n    throw new ArgumentNullException(nameof(customer));\n}";

        [Fact]
        public void EveryLineGetsTheIndentOfTheTag()
        {
            const string template = "    public void Save(Customer customer)\n    {\n        <%PROCESS_TEMPLATE check%>\n    }\n";

            Assert.Equal(
                "    public void Save(Customer customer)\n    {\n" +
                "        if (customer == null)\n        {\n            throw new ArgumentNullException(nameof(customer));\n        }\n" +
                "    }\n",
                Run(template, new() { ["check"] = NullCheck }));
        }

        [Fact]
        public void CrLfLines()
        {
            const string template = "{\r\n    <%PROCESS_TEMPLATE s%>\r\n}\r\n";

            Assert.Equal("{\r\n    a\r\n      b\r\n}\r\n", Run(template, new() { ["s"] = "a\r\n  b" }));
        }

        [Fact]
        public void TabsWork()
        {
            Assert.Equal("\t\ta\n\t\tb\n", Run("\t\t<%PROCESS_TEMPLATE s%>\n", new() { ["s"] = "a\nb" }));
        }

        [Fact]
        public void EmptyLinesGetNoIndent()
        {
            Assert.Equal("  a\n\n  b\n\r\n  c\n", Run("  <%PROCESS_TEMPLATE s%>\n", new() { ["s"] = "a\n\nb\n\r\nc" }));
        }

        [Fact]
        public void ATrailingLineBreakOfTheSubTemplateGetsNoIndent()
        {
            Assert.Equal("  a\n  b\n\n", Run("  <%PROCESS_TEMPLATE s%>\n", new() { ["s"] = "a\nb\n" }));
        }

        [Fact]
        public void NoIndentWhenTheTagIsNotAloneOnItsLine()
        {
            Assert.Equal("  x a\nb\n", Run("  x <%PROCESS_TEMPLATE s%>\n", new() { ["s"] = "a\nb" }));
            Assert.Equal("  a\nb;\n", Run("  <%PROCESS_TEMPLATE s%>;\n", new() { ["s"] = "a\nb" }));
            Assert.Equal("a\nb\n", Run("<%PROCESS_TEMPLATE s%>\n", new() { ["s"] = "a\nb" }));
        }

        [Fact]
        public void TagOnTheFirstOrLastLineOfTheTemplate()
        {
            Assert.Equal("  a\n  b", Run("  <%PROCESS_TEMPLATE s%>", new() { ["s"] = "a\nb" }));
        }

        [Fact]
        public void NestedSubTemplatesAddTheirIndents()
        {
            var subs = new Dictionary<string, string>
            {
                ["outer"] = "begin\n  <%PROCESS_TEMPLATE inner%>\nend",
                ["inner"] = "x\ny",
            };

            Assert.Equal("    begin\n      x\n      y\n    end\n", Run("    <%PROCESS_TEMPLATE outer%>\n", subs));
        }

        [Fact]
        public void IndentedTagInsideABlock()
        {
            Assert.Equal("{\n    a\n    b\n}\n", Run("{\n<%IF yes%>\n    <%PROCESS_TEMPLATE s%>\n<%ENDIF%>\n}\n", new() { ["s"] = "a\nb" }));
        }

        [Fact]
        public void ValuesInsideTheSubTemplateAreIndentedToo()
        {
            Assert.Equal("  v\n  name\n", Run("  <%PROCESS_TEMPLATE s%>\n", new() { ["s"] = "v\n<%=name%>" }));
        }

        [Fact]
        public void RemovePreviousInsideAnIndentedSubTemplateStaysInsideIt()
        {
            Assert.Equal("keep\n  ab\n", Run("keep\n  <%PROCESS_TEMPLATE s%>\n", new() { ["s"] = "abc<%REMOVE_PREVIOUS 5%>ab" }));
        }

        [Fact]
        public void TheParserSetsTheIndentOnTheToken()
        {
            var token = (ProcessTemplateToken)new Parser().Parse("x\n \t<%PROCESS_TEMPLATE s%>\ny")[1];

            Assert.Equal(" \t", token.Indent);
            Assert.Equal("", ((ProcessTemplateToken)new Parser().Parse("x <%PROCESS_TEMPLATE s%>")[1]).Indent);
        }
    }
}
