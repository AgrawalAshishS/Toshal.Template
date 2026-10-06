extern alias generator;

using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using Toshal.Template.Compiled.Tests.Support;

using Xunit;

using TemplateGenerator = generator::Toshal.Template.Generator.TemplateGenerator;

namespace Toshal.Template.Compiled.Tests
{
    // Runs the source generator the way the compiler does, with .ctt files as AdditionalFiles and the MSBuild values the package passes.
    public class GeneratorTests
    {
        private static readonly string ProjectDir = Path.Combine(Path.GetTempPath(), "ToshalProject");

        [Fact]
        public void ClassNameAndNamespaceComeFromThePath()
        {
            var run = Run(new[] { Template("Templates/Email/OrderConfirm.ctt", "Hi <%=name%>") }, rootNamespace: "MyApp");

            var source = Assert.Single(run.Trees);
            Assert.EndsWith("Templates_Email_OrderConfirm.ctt.g.cs", source.FilePath, StringComparison.Ordinal);
            Assert.Contains("namespace MyApp.Templates.Email", source.ToString(), StringComparison.Ordinal);
            Assert.Contains("partial class OrderConfirm : global::Toshal.Template.Compiled.CompiledTemplate", source.ToString(), StringComparison.Ordinal);
            Assert.Contains("Made by Toshal.Template from Templates/Email/OrderConfirm.ctt.", source.ToString(), StringComparison.Ordinal);
        }

        [Fact]
        public void MetadataChangesTheClassNameAndNamespace()
        {
            var run = Run(new[] { Template("Templates/Mail.ctt", "x", className: "MailTemplate", classNamespace: "Shop.Mail") }, rootNamespace: "MyApp");

            var text = Assert.Single(run.Trees).ToString();
            Assert.Contains("namespace Shop.Mail", text, StringComparison.Ordinal);
            Assert.Contains("partial class MailTemplate ", text, StringComparison.Ordinal);
        }

        [Fact]
        public void OnlyCttFilesAreUsed()
        {
            var run = Run(new[] { Template("readme.txt", "<%=x%>"), Template("A.CTT", "a") });

            Assert.Single(run.Trees);
        }

        [Fact]
        public void TheGeneratedClassCompilesWithTheOtherHalfAndWrites()
        {
            const string half = @"
namespace MyApp
{
    public partial class Hello
    {
        private partial string? TokenValue(Toshal.Template.TokenArgs args) => args.Name == ""name"" ? (string?)args.Context : null;
    }
}";
            var run = Run(new[] { Template("Hello.ctt", "Hello <%=Name%>!") }, rootNamespace: "MyApp", sources: half);

            Assert.Empty(run.Diagnostics);
            var template = Load(run.Compilation, "MyApp.Hello");
            Assert.Equal("Hello Asha!", template.Process("Asha").ToString());
        }

        [Fact]
        public void AWriteTokenMethodOfTheOtherHalfIsUsedInsteadOfTokenValue()
        {
            const string half = @"
namespace MyApp
{
    public partial class Hello
    {
        private partial void WriteToken(Toshal.Template.TokenArgs args, System.Text.StringBuilder output) => output.Append(args.Name.Length);
    }
}";
            var run = Run(new[] { Template("Hello.ctt", "<%=abc%>") }, rootNamespace: "MyApp", sources: half);

            Assert.Empty(run.Diagnostics);
            Assert.Contains("this.WriteToken(", Assert.Single(run.Trees).ToString(), StringComparison.Ordinal);
            Assert.Equal("3", Load(run.Compilation, "MyApp.Hello").Process().ToString());
        }

        [Fact]
        public void AParserErrorIsReportedAtItsLineAndColumn()
        {
            var run = Run(new[] { Template("Bad.ctt", "line 1\r\n  <%IF a%>no end") });

            var error = Assert.Single(run.Result.Diagnostics);
            Assert.Equal("TTC001", error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
            Assert.Contains("Bad.ctt: ", error.GetMessage(), StringComparison.Ordinal);
            var span = error.Location.GetLineSpan();
            Assert.Equal(Path.Combine(ProjectDir, "Bad.ctt"), span.Path);
            Assert.Equal(1, span.StartLinePosition.Line);
            Assert.Equal(2, span.StartLinePosition.Character);
            Assert.Empty(run.Trees);
        }

        [Fact]
        public void ABadClassNameIsReported()
        {
            var run = Run(new[] { Template("Mail.ctt", "x", className: "1 Mail") });

            Assert.Equal("TTC002", Assert.Single(run.Result.Diagnostics).Id);
        }

        [Fact]
        public void AFileNameThatIsNotValidCSharpBecomesAValidName()
        {
            var run = Run(new[] { Template("my-folder/2nd order.ctt", "x") }, rootNamespace: "App");

            var text = Assert.Single(run.Trees).ToString();
            Assert.Contains("namespace App.my_folder", text, StringComparison.Ordinal);
            Assert.Contains("partial class _2nd_order ", text, StringComparison.Ordinal);
        }

        [Fact]
        public void WithoutARootNamespaceTheFoldersAreTheNamespace()
        {
            var run = Run(new[] { Template("Top.ctt", "x"), Template("Sub/Inner.ctt", "y") });

            var texts = run.Trees.Select(t => t.ToString()).ToList();
            Assert.Contains(texts, t => t.Contains("partial class Top ", StringComparison.Ordinal) && !t.Contains("namespace", StringComparison.Ordinal));
            Assert.Contains(texts, t => t.Contains("namespace Sub", StringComparison.Ordinal));
            Assert.Empty(run.Diagnostics);
        }

        private sealed record GeneratorRun(GeneratorRunResult Result, Compilation Compilation, ImmutableArray<Diagnostic> Diagnostics)
        {
            public List<SyntaxTree> Trees => this.Compilation.SyntaxTrees.Where(t => t.FilePath.EndsWith(".g.cs", StringComparison.Ordinal)).ToList();
        }

        private static GeneratorRun Run(IEnumerable<TestTemplate> templates, string? rootNamespace = null, string sources = "")
        {
            var list = templates.ToList();
            var compilation = CSharpCompilation.Create(
                "Test" + Guid.NewGuid().ToString("N"),
                sources.Length > 0 ? new[] { CSharpSyntaxTree.ParseText(sources) } : Array.Empty<SyntaxTree>(),
                CompiledHarness.MetadataReferences,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

            var global = new Dictionary<string, string> { ["build_property.MSBuildProjectDirectory"] = ProjectDir };
            if (rootNamespace != null) global["build_property.RootNamespace"] = rootNamespace;

            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                new[] { new TemplateGenerator().AsSourceGenerator() },
                list.Select(t => (AdditionalText)t),
                optionsProvider: new TestOptionsProvider(global, list));

            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
            var result = driver.GetRunResult().Results.Single();
            var problems = output.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning).ToImmutableArray();
            return new GeneratorRun(result, output, problems);
        }

        private static CompiledTemplate Load(Compilation compilation, string typeName)
        {
            using var stream = new MemoryStream();
            var emit = compilation.Emit(stream);
            Assert.True(emit.Success, string.Join("\r\n", emit.Diagnostics));
            stream.Position = 0;
            var assembly = AssemblyLoadContext.Default.LoadFromStream(stream);
            return (CompiledTemplate)Activator.CreateInstance(assembly.GetType(typeName, throwOnError: true)!)!;
        }

        private static TestTemplate Template(string relativePath, string text, string? className = null, string? classNamespace = null) =>
            new TestTemplate(Path.Combine(ProjectDir, relativePath.Replace('/', Path.DirectorySeparatorChar)), text, className, classNamespace);

        private sealed class TestTemplate : AdditionalText
        {
            private readonly string text;

            public TestTemplate(string path, string text, string? className, string? classNamespace)
            {
                this.Path = path;
                this.text = text;
                this.ClassName = className;
                this.Namespace = classNamespace;
            }

            public override string Path { get; }

            public string? ClassName { get; }

            public string? Namespace { get; }

            public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(this.text, Encoding.UTF8);
        }

        private sealed class TestOptionsProvider : AnalyzerConfigOptionsProvider
        {
            private readonly Dictionary<string, TestOptions> files;

            public TestOptionsProvider(Dictionary<string, string> global, IEnumerable<TestTemplate> templates)
            {
                this.GlobalOptions = new TestOptions(global);
                this.files = templates.ToDictionary(t => t.Path, t =>
                {
                    var values = new Dictionary<string, string>();
                    if (t.ClassName != null) values["build_metadata.AdditionalFiles.ClassName"] = t.ClassName;
                    if (t.Namespace != null) values["build_metadata.AdditionalFiles.Namespace"] = t.Namespace;
                    return new TestOptions(values);
                });
            }

            public override AnalyzerConfigOptions GlobalOptions { get; }

            public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => new TestOptions(new Dictionary<string, string>());

            public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) =>
                this.files.TryGetValue(textFile.Path, out var options) ? options : new TestOptions(new Dictionary<string, string>());
        }

        private sealed class TestOptions : AnalyzerConfigOptions
        {
            private readonly Dictionary<string, string> values;

            public TestOptions(Dictionary<string, string> values)
            {
                this.values = values;
            }

            public override bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? value) => this.values.TryGetValue(key, out value);
        }
    }
}
