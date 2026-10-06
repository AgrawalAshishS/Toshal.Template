extern alias codegen;
extern alias generator;

using System.Collections;
using System.Runtime.Loader;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Toshal.Template.Compiled.Tests.Support;

using Xunit;

using CreateTemplateStubs = generator::Toshal.Template.Generator.CreateTemplateStubs;
using CSharpEmitter = codegen::Toshal.Template.CodeGen.CSharpEmitter;
using StubEmitter = codegen::Toshal.Template.CodeGen.StubEmitter;
using TemplateNaming = codegen::Toshal.Template.CodeGen.TemplateNaming;
using TemplateSource = codegen::Toshal.Template.CodeGen.TemplateSource;

namespace Toshal.Template.Compiled.Tests
{
    // The other half of the class that is made once for the user, the names that come from the path, and the MSBuild task that writes the stubs.
    public class StubTests
    {
        public static IEnumerable<object[]> Templates => FeatureCasesTests.Cases.Select((t, i) => new object[] { i });

        // A stub as made, without changes, compiles with the generated half without a warning, and writes what Processor writes when every
        // provider answers "nothing".
        [Theory]
        [MemberData(nameof(Templates))]
        public void AFreshStubCompilesCleanlyAndAnswersNothing(int index)
        {
            var template = FeatureCasesTests.Cases[index];
            foreach (var writer in new[] { false, true })
            {
                var source = new TemplateSource("Stubs", "S" + index, template) { UseTokenWriter = writer };
                var code = CSharpEmitter.Emit(source);
                var stub = StubEmitter.Emit(source, code);

                var compilation = CSharpCompilation.Create(
                    "Stub" + Guid.NewGuid().ToString("N"),
                    new[] { CSharpSyntaxTree.ParseText(code.Code), CSharpSyntaxTree.ParseText(stub) },
                    CompiledHarness.MetadataReferences,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

                using var stream = new MemoryStream();
                var emit = compilation.Emit(stream);
                var problems = emit.Diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning).ToList();
                Assert.True(problems.Count == 0, string.Join("\r\n", problems) + "\r\n" + stub);

                stream.Position = 0;
                var type = AssemblyLoadContext.Default.LoadFromStream(stream).GetType("Stubs.S" + index, throwOnError: true)!;
                var actual = ((CompiledTemplate)Activator.CreateInstance(type)!).Process().ToString();

                var processor = new Processor
                {
                    TokenValueProvider = _ => null,
                    ConditionValueProvider = _ => false,
                    LoopValueProvider = _ => (IList?)null,
                    WithValueProvider = _ => null,
                    ProcessTemplateValueProvider = _ => null,
                };
                Assert.Equal(processor.Process(new ProcessorArgs(new Parser().Parse(template))).ToString(), actual);
            }
        }

        [Fact]
        public void TheStubListsEveryNameOnce()
        {
            var source = new TemplateSource("N", "C", "<%=b%><%=a%><%=b%><%IF x%><%ENDIF%><%FOREACH rows%><%ENDFOR%><%REUSE_FOREACH rows again%><%WITH w%><%ENDWITH%><%PROCESS_TEMPLATE f%>");
            var stub = StubEmitter.Emit(source, CSharpEmitter.Emit(source));

            Assert.Equal(1, Count(stub, "\"a\" => null"));
            Assert.Equal(1, Count(stub, "\"b\" => null"));
            Assert.True(stub.IndexOf("\"a\" =>", StringComparison.Ordinal) < stub.IndexOf("\"b\" =>", StringComparison.Ordinal), "sorted");
            Assert.Contains("\"x\" => false", stub, StringComparison.Ordinal);
            Assert.Contains("\"again\" => null", stub, StringComparison.Ordinal);
            Assert.Contains("\"rows\" => null", stub, StringComparison.Ordinal);
            Assert.Contains("\"w\" => null", stub, StringComparison.Ordinal);
            Assert.Contains("\"f\" => null", stub, StringComparison.Ordinal);
            Assert.Contains("using Toshal.Template.Compiled;", stub, StringComparison.Ordinal);
        }

        [Fact]
        public void AStaticTemplateHasAnEmptyStubAndNoPartialMethods()
        {
            var source = new TemplateSource(string.Empty, "Plain", "just text");
            var code = CSharpEmitter.Emit(source);
            var stub = StubEmitter.Emit(source, code);

            Assert.DoesNotContain("partial string", code.Code, StringComparison.Ordinal);
            Assert.DoesNotContain("namespace", stub, StringComparison.Ordinal);
            Assert.DoesNotContain("using", stub, StringComparison.Ordinal);
            Assert.Contains("public partial class Plain", stub, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("MyApp", "Templates/Email/OrderConfirm.ctt", "MyApp.Templates.Email", "OrderConfirm")]
        [InlineData("MyApp", "OrderConfirm.ctt", "MyApp", "OrderConfirm")]
        [InlineData("", "a/b/c.ctt", "a.b", "c")]
        [InlineData(null, "c.ctt", "", "c")]
        [InlineData("My.App", "1st/class.ctt", "My.App._1st", "@class")]
        [InlineData("App", "with space/x-y.z.ctt", "App.with_space", "x_y_z")]
        public void NamesComeFromThePath(string? rootNamespace, string path, string expectedNamespace, string expectedClass)
        {
            var project = Path.Combine(Path.GetTempPath(), "proj");
            var names = TemplateNaming.FromPath(rootNamespace, project, Path.Combine(project, path));

            Assert.Equal(expectedNamespace, names.Namespace);
            Assert.Equal(expectedClass, names.ClassName);
        }

        [Fact]
        public void ATemplateOutsideTheProjectFolderGetsTheRootNamespace()
        {
            var names = TemplateNaming.FromPath("App", Path.Combine(Path.GetTempPath(), "proj"), Path.Combine(Path.GetTempPath(), "other", "x.ctt"));

            Assert.Equal(("App", "x"), names);
        }

        [Theory]
        [InlineData("Order", false, true)]
        [InlineData("@class", false, true)]
        [InlineData("class", false, false)]
        [InlineData("1x", false, false)]
        [InlineData("a.b", false, false)]
        [InlineData("a.b", true, true)]
        [InlineData("a..b", true, false)]
        [InlineData("", false, false)]
        public void ValidNames(string name, bool dotted, bool valid)
        {
            Assert.Equal(valid, TemplateNaming.IsValidName(name, dotted));
        }

        [Fact]
        public void TheTaskWritesAMissingStubOnceAndNeverChangesIt()
        {
            var project = Directory.CreateTempSubdirectory("ToshalStubs").FullName;
            try
            {
                Directory.CreateDirectory(Path.Combine(project, "Mail"));
                File.WriteAllText(Path.Combine(project, "Mail", "Welcome.ctt"), "Hi <%=name%>");
                File.WriteAllText(Path.Combine(project, "Bad.ctt"), "<%IF a%>");
                File.WriteAllText(Path.Combine(project, "notes.txt"), "x");

                var first = RunTask(project, "Mail/Welcome.ctt", "Bad.ctt", "notes.txt");
                var stubPath = Path.Combine(project, "Mail", "Welcome.cs");
                Assert.Equal(new[] { stubPath }, first);
                var stub = File.ReadAllText(stubPath);
                Assert.Contains("namespace Shop.Mail", stub, StringComparison.Ordinal);
                Assert.Contains("public partial class Welcome", stub, StringComparison.Ordinal);
                Assert.Contains("\"name\" => null", stub, StringComparison.Ordinal);
                Assert.False(File.Exists(Path.Combine(project, "Bad.cs")));

                File.WriteAllText(stubPath, "// mine");
                Assert.Empty(RunTask(project, "Mail/Welcome.ctt"));
                Assert.Equal("// mine", File.ReadAllText(stubPath));
            }
            finally
            {
                Directory.Delete(project, recursive: true);
            }
        }

        [Fact]
        public void TheTaskUsesTheClassNameAndNamespaceMetadata()
        {
            var project = Directory.CreateTempSubdirectory("ToshalStubs").FullName;
            try
            {
                File.WriteAllText(Path.Combine(project, "Mail.ctt"), "x");
                var item = new TaskItem(Path.Combine(project, "Mail.ctt"));
                item.SetMetadata("ClassName", "MailTemplate");
                item.SetMetadata("Namespace", "Other");

                var task = new CreateTemplateStubs { BuildEngine = new TestEngine(), ProjectDirectory = project, RootNamespace = "Shop", Templates = new ITaskItem[] { item } };
                Assert.True(task.Execute());

                var stub = File.ReadAllText(Path.Combine(project, "Mail.cs"));
                Assert.Contains("namespace Other", stub, StringComparison.Ordinal);
                Assert.Contains("public partial class MailTemplate", stub, StringComparison.Ordinal);
            }
            finally
            {
                Directory.Delete(project, recursive: true);
            }
        }

        private static string[] RunTask(string project, params string[] files)
        {
            var task = new CreateTemplateStubs
            {
                BuildEngine = new TestEngine(),
                ProjectDirectory = project,
                RootNamespace = "Shop",
                Templates = files.Select(f => (ITaskItem)new TaskItem(Path.Combine(project, f))).ToArray(),
            };

            Assert.True(task.Execute());
            return task.CreatedFiles.Select(i => i.ItemSpec).ToArray();
        }

        private static int Count(string text, string part) => (text.Length - text.Replace(part, string.Empty, StringComparison.Ordinal).Length) / part.Length;

        private sealed class TestEngine : IBuildEngine
        {
            public bool ContinueOnError => false;

            public int LineNumberOfTaskNode => 0;

            public int ColumnNumberOfTaskNode => 0;

            public string ProjectFileOfTaskNode => string.Empty;

            public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs) => true;

            public void LogCustomEvent(CustomBuildEventArgs e)
            {
            }

            public void LogErrorEvent(BuildErrorEventArgs e)
            {
            }

            public void LogMessageEvent(BuildMessageEventArgs e)
            {
            }

            public void LogWarningEvent(BuildWarningEventArgs e)
            {
            }
        }
    }
}
