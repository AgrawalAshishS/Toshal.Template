extern alias codegen;

using System.Runtime.CompilerServices;
using System.Text;

using Xunit;

using CSharpEmitter = codegen::Toshal.Template.CodeGen.CSharpEmitter;
using StubEmitter = codegen::Toshal.Template.CodeGen.StubEmitter;
using TemplateSource = codegen::Toshal.Template.CodeGen.TemplateSource;

namespace Toshal.Template.Compiled.Tests
{
    // The files the emitters write, byte for byte, kept in Golden/. A change to how the emitters are written must not change their output.
    // To accept a wanted change, set the environment variable TOSHAL_UPDATE_GOLDEN=1, run these tests once and look at the diff.
    public class EmitterOutputTests
    {
        // Templates for the generated half: no tags, attributes, every kind of tag, and SET, FOREACH and sub templates together.
        private static readonly string[] CodeCases =
        {
            "just text",
            "The <%=Product abc=\"xyz\" %> team",
            "<%=b%><%=a%><%=b%><%IF x%><%ENDIF%><%FOREACH rows%><%ENDFOR%><%REUSE_FOREACH rows again%><%WITH w%><%ENDWITH%><%PROCESS_TEMPLATE f%>",
            "<%SET x%>outer<%ENDSET%><%FOREACH rows q=\"1\"%><%=name%><%SEPARATOR%>,<%ENDSEPARATOR%><%ENDFOR%><%PROCESS_TEMPLATE footer%>[<%=x%>]",
        };

        [Fact]
        public void TheStubsAreUnchanged()
        {
            var text = new StringBuilder();
            var templates = FeatureCasesTests.Cases.Concat(CodeCases).ToArray();
            for (int i = 0; i < templates.Length; i++)
            {
                // The global namespace only for the short list; the long one would add much text and no new case.
                foreach (var (source, label) in Sources(templates[i], i).Where(s => i >= FeatureCasesTests.Cases.Length || s.Source.Namespace.Length > 0))
                {
                    text.Append("==== ").Append(label).Append(" ====\r\n");
                    text.Append(StubEmitter.Emit(source, CSharpEmitter.Emit(source)));
                }
            }

            Check("Stubs.txt", text.ToString());
        }

        [Fact]
        public void TheGeneratedCodeIsUnchanged()
        {
            var text = new StringBuilder();
            for (int i = 0; i < CodeCases.Length; i++)
            {
                foreach (var (source, label) in Sources(CodeCases[i], i))
                {
                    text.Append("==== ").Append(label).Append(" ====\r\n");
                    text.Append(CSharpEmitter.Emit(source).Code);
                }
            }

            Check("Code.txt", text.ToString());
        }

        // Each template with and without a namespace, with TokenValue and with WriteToken.
        private static IEnumerable<(TemplateSource Source, string Label)> Sources(string template, int index)
        {
            foreach (var ns in new[] { "Stubs", string.Empty })
            {
                foreach (var writer in new[] { false, true })
                {
                    var source = new TemplateSource(ns, "S" + index, template) { SourcePath = "T" + index + ".ctt", UseTokenWriter = writer };
                    yield return (source, index + (ns.Length > 0 ? " namespace" : " global") + (writer ? " writer" : " value"));
                }
            }
        }

        private static void Check(string fileName, string actual, [CallerFilePath] string thisFile = "")
        {
            var path = Path.Combine(Path.GetDirectoryName(thisFile)!, "Golden", fileName);
            if (Environment.GetEnvironmentVariable("TOSHAL_UPDATE_GOLDEN") == "1")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, actual);
                return;
            }

            Assert.True(File.Exists(path), "Missing " + path + ". Run with TOSHAL_UPDATE_GOLDEN=1 once to make it.");
            Assert.Equal(File.ReadAllText(path), actual);
        }
    }
}
