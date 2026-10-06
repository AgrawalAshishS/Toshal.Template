extern alias codegen;

using Xunit;

using CSharpEmitter = codegen::Toshal.Template.CodeGen.CSharpEmitter;
using TemplateSource = codegen::Toshal.Template.CodeGen.TemplateSource;

namespace Toshal.Template.Compiled.Tests
{
    // REMOVE_PREVIOUS and REMOVE_PREVIOUS_NEW_LINE are done at build time when the chars they remove are template text, so the generated code
    // just writes the shorter text. Only chars that come from data at run time need a call. FeatureCasesTests proves the text stays the same.
    public class FoldingTests
    {
        [Theory]
        [InlineData("a,b,<%REMOVE_PREVIOUS 1%>", "output.Append(\"a,b\");")]
        [InlineData("ab<%REMOVE_PREVIOUS 2%>c", "output.Append(\"c\");")]
        [InlineData("line\r\n<%REMOVE_PREVIOUS_NEW_LINE%>end", "output.Append(\"lineend\");")]
        [InlineData("line\n<%REMOVE_PREVIOUS_NEW_LINE%>end", "output.Append(\"lineend\");")]
        [InlineData("line\r<%REMOVE_PREVIOUS_NEW_LINE%>end", "output.Append(\"lineend\");")]
        [InlineData("line<%REMOVE_PREVIOUS_NEW_LINE%>end", "output.Append(\"lineend\");")]
        [InlineData("a\r\n\r\n<%REMOVE_PREVIOUS_NEW_LINE%><%REMOVE_PREVIOUS_NEW_LINE%>b", "output.Append(\"ab\");")]
        public void TemplateTextIsRemovedAtBuildTime(string template, string expectedLine)
        {
            var code = Emit(template);

            Assert.Contains(expectedLine, code, StringComparison.Ordinal);
            Assert.DoesNotContain("RemovePrevious", code, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("<%=x%>ab<%REMOVE_PREVIOUS 3%>", "run.RemovePrevious(output, 1);")]
        [InlineData("<%=x%><%REMOVE_PREVIOUS 2%>", "run.RemovePrevious(output, 2);")]
        [InlineData("<%=x%>\n<%REMOVE_PREVIOUS_NEW_LINE%>", "run.RemovePreviousNewLine(output);")]
        [InlineData("<%=x%><%REMOVE_PREVIOUS_NEW_LINE%>", "run.RemovePreviousNewLine(output);")]
        public void CharsThatComeFromDataAreRemovedAtRunTime(string template, string expectedLine)
        {
            var code = Emit(template);

            Assert.Contains(expectedLine, code, StringComparison.Ordinal);
            Assert.DoesNotContain("\"ab\"", code, StringComparison.Ordinal);
        }

        private static string Emit(string template) => CSharpEmitter.Emit(new TemplateSource("N", "C", template)).Code;
    }
}
