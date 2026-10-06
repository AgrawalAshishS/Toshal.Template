extern alias generator;

using System.Runtime.Loader;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Toshal.Template.Compiled.Tests.Support;

using Xunit;

using GenerateTemplates = generator::Toshal.Template.Generator.GenerateTemplates;

namespace Toshal.Template.Compiled.Tests
{
    // Runs the MSBuild task of the package the way the build does: it writes Name.g.cs (and the stub Name.cs) next to each .ctt file.
    public sealed class GeneratorTests : IDisposable
    {
        private readonly string project = Directory.CreateTempSubdirectory("ToshalProject").FullName;

        public void Dispose() => Directory.Delete(this.project, recursive: true);

        [Fact]
        public void ClassNameAndNamespaceComeFromThePath()
        {
            var run = this.Run(new[] { this.Template("Templates/Email/OrderConfirm.ctt", "Hi <%=name%>") }, rootNamespace: "MyApp");

            var path = this.PathOf("Templates/Email/OrderConfirm.g.cs");
            Assert.Equal(new[] { path }, run.Task.GeneratedFiles.Select(i => i.ItemSpec));
            var text = File.ReadAllText(path);
            Assert.Contains("namespace MyApp.Templates.Email", text, StringComparison.Ordinal);
            Assert.Contains("partial class OrderConfirm : global::Toshal.Template.Compiled.CompiledTemplate", text, StringComparison.Ordinal);
            Assert.Contains("Made by Toshal.Template from Templates/Email/OrderConfirm.ctt.", text, StringComparison.Ordinal);
        }

        [Fact]
        public void MetadataChangesTheClassNameAndNamespace()
        {
            this.Run(new[] { this.Template("Templates/Mail.ctt", "x", className: "MailTemplate", classNamespace: "Shop.Mail") }, rootNamespace: "MyApp");

            var text = File.ReadAllText(this.PathOf("Templates/Mail.g.cs"));
            Assert.Contains("namespace Shop.Mail", text, StringComparison.Ordinal);
            Assert.Contains("partial class MailTemplate ", text, StringComparison.Ordinal);
        }

        [Fact]
        public void OnlyCttFilesAreUsed()
        {
            var run = this.Run(new[] { this.Template("readme.txt", "<%=x%>"), this.Template("A.CTT", "a") });

            Assert.Equal(new[] { this.PathOf("A.g.cs") }, run.Task.GeneratedFiles.Select(i => i.ItemSpec));
            Assert.False(File.Exists(this.PathOf("readme.g.cs")));
        }

        [Fact]
        public void TheGeneratedClassCompilesWithTheOtherHalfAndWrites()
        {
            File.WriteAllText(this.PathOf("Hello.cs"), @"
namespace MyApp
{
    public partial class Hello
    {
        private partial string? TokenValue(Toshal.Template.TokenArgs args) => args.Name == ""name"" ? (string?)args.Context : null;
    }
}");
            this.Run(new[] { this.Template("Hello.ctt", "Hello <%=Name%>!") }, rootNamespace: "MyApp");

            Assert.Equal("Hello Asha!", this.Load("MyApp.Hello").Process("Asha").ToString());
        }

        [Fact]
        public void AWriteTokenMethodOfTheOtherHalfIsUsedInsteadOfTokenValue()
        {
            File.WriteAllText(this.PathOf("Hello.cs"), @"
namespace MyApp
{
    public partial class Hello
    {
        private partial void WriteToken(Toshal.Template.TokenArgs args, System.Text.StringBuilder output) => output.Append(args.Name.Length);
    }
}");
            this.Run(new[] { this.Template("Hello.ctt", "<%=abc%>") }, rootNamespace: "MyApp");

            Assert.Contains("this.WriteToken(", File.ReadAllText(this.PathOf("Hello.g.cs")), StringComparison.Ordinal);
            Assert.Equal("3", this.Load("MyApp.Hello").Process().ToString());
        }

        [Fact]
        public void AParserErrorIsReportedAtItsLineAndColumn()
        {
            var run = this.Run(new[] { this.Template("Bad.ctt", "line 1\r\n  <%IF a%>no end") }, succeeds: false);

            var error = Assert.Single(run.Engine.Errors);
            Assert.Equal("TTC001", error.Code);
            Assert.Contains("Bad.ctt: ", error.Message, StringComparison.Ordinal);
            Assert.Equal(this.PathOf("Bad.ctt"), error.File);
            Assert.Equal(2, error.LineNumber);
            Assert.Equal(3, error.ColumnNumber);
            Assert.False(File.Exists(this.PathOf("Bad.g.cs")));
            Assert.False(File.Exists(this.PathOf("Bad.cs")));
        }

        [Fact]
        public void ABadClassNameIsReported()
        {
            var run = this.Run(new[] { this.Template("Mail.ctt", "x", className: "1 Mail") }, succeeds: false);

            var error = Assert.Single(run.Engine.Errors);
            Assert.Equal("TTC002", error.Code);
            Assert.Equal(this.PathOf("Mail.ctt"), error.File);
        }

        [Fact]
        public void AFileNameThatIsNotValidCSharpBecomesAValidName()
        {
            this.Run(new[] { this.Template("my-folder/2nd order.ctt", "x") }, rootNamespace: "App");

            var text = File.ReadAllText(this.PathOf("my-folder/2nd order.g.cs"));
            Assert.Contains("namespace App.my_folder", text, StringComparison.Ordinal);
            Assert.Contains("partial class _2nd_order ", text, StringComparison.Ordinal);
        }

        [Fact]
        public void WithoutARootNamespaceTheFoldersAreTheNamespace()
        {
            this.Run(new[] { this.Template("Top.ctt", "x"), this.Template("Sub/Inner.ctt", "y") });

            var top = File.ReadAllText(this.PathOf("Top.g.cs"));
            Assert.Contains("partial class Top ", top, StringComparison.Ordinal);
            Assert.DoesNotContain("namespace", top, StringComparison.Ordinal);
            Assert.Contains("namespace Sub", File.ReadAllText(this.PathOf("Sub/Inner.g.cs")), StringComparison.Ordinal);
            this.Compile();
        }

        // The generated file is written again only when its text changes, so an unchanged template does not make the project build again.
        [Fact]
        public void AnUnchangedGeneratedFileIsNotWrittenAgain()
        {
            this.Run(new[] { this.Template("Mail.ctt", "Hi") });
            var path = this.PathOf("Mail.g.cs");
            var old = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(path, old);

            this.Run(new[] { this.Template("Mail.ctt", "Hi") });
            Assert.Equal(old, File.GetLastWriteTimeUtc(path));

            this.Run(new[] { this.Template("Mail.ctt", "Hello") });
            Assert.NotEqual(old, File.GetLastWriteTimeUtc(path));
            Assert.Contains("Hello", File.ReadAllText(path), StringComparison.Ordinal);
        }

        // A generated file that someone changed by hand is made again from the template.
        [Fact]
        public void AChangedGeneratedFileIsMadeAgain()
        {
            this.Run(new[] { this.Template("Mail.ctt", "Hi") });
            var path = this.PathOf("Mail.g.cs");
            var made = File.ReadAllText(path);

            File.WriteAllText(path, "// changed");
            this.Run(new[] { this.Template("Mail.ctt", "Hi") });

            Assert.Equal(made, File.ReadAllText(path));
        }

        [Fact]
        public void CreateStubsFalseWritesOnlyTheGeneratedFile()
        {
            var run = this.Run(new[] { this.Template("Mail.ctt", "Hi <%=name%>") }, createStubs: false);

            Assert.True(File.Exists(this.PathOf("Mail.g.cs")));
            Assert.False(File.Exists(this.PathOf("Mail.cs")));
            Assert.Empty(run.Task.CreatedFiles);
        }

        private sealed record TaskRun(GenerateTemplates Task, TestBuildEngine Engine);

        private TaskRun Run(IEnumerable<ITaskItem> templates, string? rootNamespace = null, bool createStubs = true, bool succeeds = true)
        {
            var engine = new TestBuildEngine();
            var task = new GenerateTemplates
            {
                BuildEngine = engine,
                ProjectDirectory = this.project,
                RootNamespace = rootNamespace,
                CreateStubs = createStubs,
                Templates = templates.ToArray(),
            };

            Assert.Equal(succeeds, task.Execute());
            return new TaskRun(task, engine);
        }

        private string PathOf(string relativePath) => Path.Combine(this.project, relativePath.Replace('/', Path.DirectorySeparatorChar));

        private ITaskItem Template(string relativePath, string text, string? className = null, string? classNamespace = null)
        {
            var path = this.PathOf(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text);
            var item = new TaskItem(path);
            if (className != null) item.SetMetadata("ClassName", className);
            if (classNamespace != null) item.SetMetadata("Namespace", classNamespace);
            return item;
        }

        // Compiles every .cs file of the project folder, as the build would.
        private Compilation Compile()
        {
            var trees = Directory.EnumerateFiles(this.project, "*.cs", SearchOption.AllDirectories)
                .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f), path: f));
            var compilation = CSharpCompilation.Create(
                "Test" + Guid.NewGuid().ToString("N"),
                trees,
                CompiledHarness.MetadataReferences,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
            Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning));
            return compilation;
        }

        private CompiledTemplate Load(string typeName)
        {
            using var stream = new MemoryStream();
            var emit = this.Compile().Emit(stream);
            Assert.True(emit.Success, string.Join("\r\n", emit.Diagnostics));
            stream.Position = 0;
            var assembly = AssemblyLoadContext.Default.LoadFromStream(stream);
            return (CompiledTemplate)Activator.CreateInstance(assembly.GetType(typeName, throwOnError: true)!)!;
        }
    }
}
