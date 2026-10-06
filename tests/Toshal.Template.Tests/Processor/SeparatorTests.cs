using System.Collections.Generic;

using Toshal.Template.Exceptions;
using Xunit;

namespace Toshal.Template.Tests
{
    // <%SEPARATOR%>...<%ENDSEPARATOR%> is written in every row of the innermost FOREACH except the last one.
    public class SeparatorTests
    {
        private static string Run(string template, params string[] rows)
        {
            var processor = new Processor
            {
                LoopValueProvider = args => args.Name switch
                {
                    "none" => new List<string>(),
                    "one" => new List<string> { "x" },
                    "inner" => new List<string> { "1", "2" },
                    _ => new List<string>(rows.Length == 0 ? new[] { "a", "b", "c" } : rows),
                },
                ConditionValueProvider = args => true,
                WithValueProvider = args => args.Context,
                TokenValueProvider = args => args.Name == "sep" ? " | " : args.Context?.ToString(),
                ProcessTemplateValueProvider = args => new Parser().Parse("<%CONTEXT_AS_STRING%><%SEPARATOR%>;<%ENDSEPARATOR%>"),
            };

            return processor.Process(new ProcessorArgs(new Parser().Parse(template))).ToString();
        }

        [Fact]
        public void WrittenBetweenRowsOnALine()
        {
            Assert.Equal("a, b, c", Run("<%FOREACH l%><%CONTEXT_AS_STRING%><%SEPARATOR%>, <%ENDSEPARATOR%><%ENDFOR%>"));
        }

        [Fact]
        public void OneItemPerLineWithoutCountingCharacters()
        {
            const string template = "public Customer(\n    <%FOREACH fields%>\n    int <%CONTEXT_AS_STRING%><%SEPARATOR%>,<%ENDSEPARATOR%>\n    <%ENDFOR%>\n)\n";

            Assert.Equal("public Customer(\n    int a,\n    int b,\n    int c\n)\n", Run(template));
            Assert.Equal("public Customer(\r\n    int a,\r\n    int b,\r\n    int c\r\n)\r\n", Run(template.Replace("\n", "\r\n")));
        }

        [Fact]
        public void OneRowAndNoRowsWriteNoSeparator()
        {
            Assert.Equal("x", Run("<%FOREACH one%><%CONTEXT_AS_STRING%><%SEPARATOR%>,<%ENDSEPARATOR%><%ENDFOR%>"));
            Assert.Equal("-", Run("<%FOREACH none%><%CONTEXT_AS_STRING%><%SEPARATOR%>,<%ENDSEPARATOR%><%NORECORD%>-<%SEPARATOR%>,<%ENDSEPARATOR%><%ENDNORECORD%><%ENDFOR%>"));
        }

        [Fact]
        public void WorksInEveryRowPart()
        {
            Assert.Equal("[a],{b},(c)",
                Run("<%FOREACH l%><%ROW%>[<%CONTEXT_AS_STRING%>]<%SEPARATOR%>,<%ENDSEPARATOR%><%ENDROW%><%ALTROW%>{<%CONTEXT_AS_STRING%>}<%SEPARATOR%>,<%ENDSEPARATOR%><%ENDALTROW%><%LASTROW%>(<%CONTEXT_AS_STRING%>)<%SEPARATOR%>,<%ENDSEPARATOR%><%ENDLASTROW%><%ENDFOR%>"));
            Assert.Equal("a;b;c", Run("<%FOREACH l%><%CONTEXT_AS_STRING%><%AFTERROW%><%SEPARATOR%>;<%ENDSEPARATOR%><%ENDAFTERROW%><%ENDFOR%>"));
            Assert.Equal("a;b;c", Run("<%FOREACH l%><%BEFOREROW%><%ENDBEFOREROW%><%CONTEXT_AS_STRING%><%SEPARATOR%>;<%ENDSEPARATOR%><%ENDFOR%>"));
        }

        [Fact]
        public void HeaderFooterAndOutsideALoopWriteNothing()
        {
            Assert.Equal("<abc>", Run("<%FOREACH l%><%HEADER%><<%SEPARATOR%>,<%ENDSEPARATOR%><%ENDHEADER%><%CONTEXT_AS_STRING%><%FOOTER%>><%SEPARATOR%>,<%ENDSEPARATOR%><%ENDFOOTER%><%ENDFOR%>"));
            Assert.Equal("x", Run("x<%SEPARATOR%>,<%ENDSEPARATOR%>"));
        }

        [Fact]
        public void InnerLoopHasItsOwnRows()
        {
            Assert.Equal("a(1+2); b(1+2); c(1+2)",
                Run("<%FOREACH l%><%CONTEXT_AS_STRING%>(<%FOREACH inner%><%CONTEXT_AS_STRING%><%SEPARATOR%>+<%ENDSEPARATOR%><%ENDFOR%>)<%SEPARATOR%>; <%ENDSEPARATOR%><%ENDFOR%>"));
        }

        [Fact]
        public void InnerLoopHeaderDoesNotSeeTheOuterRow()
        {
            Assert.Equal("a12b12", Run("<%FOREACH l%><%CONTEXT_AS_STRING%><%FOREACH inner%><%HEADER%><%SEPARATOR%>,<%ENDSEPARATOR%><%ENDHEADER%><%CONTEXT_AS_STRING%><%ENDFOR%><%ENDFOR%>", "a", "b"));
        }

        [Fact]
        public void WorksInsideIfAndWithInARow()
        {
            Assert.Equal("a,b,c", Run("<%FOREACH l%><%WITH w%><%IF yes%><%CONTEXT_AS_STRING%><%SEPARATOR%>,<%ENDSEPARATOR%><%ENDIF%><%ENDWITH%><%ENDFOR%>"));
        }

        [Fact]
        public void HoldsValuesAndTags()
        {
            Assert.Equal("a | b | c", Run("<%FOREACH l%><%CONTEXT_AS_STRING%><%SEPARATOR%><%=sep%><%ENDSEPARATOR%><%ENDFOR%>"));
        }

        [Fact]
        public void WorksInASubTemplateCalledFromARow()
        {
            Assert.Equal("a;b;c", Run("<%FOREACH l%><%PROCESS_TEMPLATE item%><%ENDFOR%>"));
        }

        [Fact]
        public void WorksWithReuseForEach()
        {
            Assert.Equal("a,b,c|a,b,c", Run("<%FOREACH l%><%CONTEXT_AS_STRING%><%SEPARATOR%>,<%ENDSEPARATOR%><%ENDFOR%>|<%REUSE_FOREACH l again%>"));
        }

        [Fact]
        public void SeparatorLinesAreTagLines()
        {
            Assert.Equal("a\n,\nb\n,\nc\n", Run("<%FOREACH l%>\n<%CONTEXT_AS_STRING%>\n<%SEPARATOR%>\n,\n<%ENDSEPARATOR%>\n<%ENDFOR%>\n"));
        }

        [Fact]
        public void SeparatorMustBeClosed()
        {
            Assert.Throws<TokenNotClosedException>(() => new Parser().Parse("<%FOREACH l%><%SEPARATOR%>,<%ENDFOR%>"));
            Assert.Throws<ParserException>(() => new Parser().Parse("<%FOREACH l%><%ENDSEPARATOR%><%ENDFOR%>"));
        }
    }
}
