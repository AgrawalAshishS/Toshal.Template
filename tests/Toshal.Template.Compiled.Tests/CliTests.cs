extern alias cli;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Toshal.Template.Compiled.Tests.Support;

using Xunit;

using CliProgram = cli::Toshal.Template.Cli.CliProgram;

namespace Toshal.Template.Compiled.Tests
{
    // The toshal-template tool in a project folder made for each test.
    public sealed class CliTests : IDisposable
    {
        private readonly string project = Directory.CreateTempSubdirectory("ToshalCli").FullName;

        public CliTests()
        {
            File.WriteAllText(Path.Combine(this.project, "Shop.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><RootNamespace>Shop.App</RootNamespace></PropertyGroup></Project>");
            Directory.CreateDirectory(Path.Combine(this.project, "Templates", "Mail"));
            File.WriteAllText(Path.Combine(this.project, "Templates", "Mail", "Welcome.ctt"), "Hi <%=name%><%IF vip%>!<%ENDIF%>");
            File.WriteAllText(Path.Combine(this.project, "Templates", "Plain.ctt"), "plain");
        }

        public void Dispose() => Directory.Delete(this.project, recursive: true);

        [Fact]
        public void AFolderGivesEveryTemplateItsClassAndStub()
        {
            var (code, text, _) = Run("generate", Path.Combine(this.project, "Templates"));

            Assert.Equal(0, code);
            Assert.Contains("2 of 2 templates done.", text, StringComparison.Ordinal);
            var generated = File.ReadAllText(Path.Combine(this.project, "Templates", "Mail", "Welcome.g.cs"));
            Assert.Contains("namespace Shop.App.Templates.Mail", generated, StringComparison.Ordinal);
            Assert.Contains("Made by Toshal.Template from Templates/Mail/Welcome.ctt.", generated, StringComparison.Ordinal);
            var stub = File.ReadAllText(Path.Combine(this.project, "Templates", "Mail", "Welcome.cs"));
            Assert.Contains("\"vip\" => false", stub, StringComparison.Ordinal);
            Assert.True(File.Exists(Path.Combine(this.project, "Templates", "Plain.g.cs")));

            // The two files compile together.
            var compilation = CSharpCompilation.Create(
                "Cli",
                new[] { CSharpSyntaxTree.ParseText(generated), CSharpSyntaxTree.ParseText(stub) },
                CompiledHarness.MetadataReferences,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
            Assert.Empty(compilation.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning));
        }

        [Fact]
        public void ARunAgainKeepsTheStubAndWritesTheClassOnlyWhenItChanges()
        {
            var template = Path.Combine(this.project, "Templates", "Plain.ctt");
            Run("generate", template);
            var stub = Path.Combine(this.project, "Templates", "Plain.cs");
            File.WriteAllText(stub, "// mine");

            var (_, text, _) = Run("generate", template);
            Assert.DoesNotContain("Wrote", text, StringComparison.Ordinal);
            Assert.Equal("// mine", File.ReadAllText(stub));

            File.WriteAllText(template, "changed <%=x%>");
            (_, text, _) = Run("generate", template);
            Assert.Contains("Wrote", text, StringComparison.Ordinal);
            Assert.Equal("// mine", File.ReadAllText(stub));
        }

        [Fact]
        public void OptionsChangeTheNames()
        {
            var (code, _, _) = Run("generate", Path.Combine(this.project, "Templates", "Plain.ctt"), "--class", "PlainText", "--namespace", "Other", "--no-stub");

            Assert.Equal(0, code);
            var generated = File.ReadAllText(Path.Combine(this.project, "Templates", "Plain.g.cs"));
            Assert.Contains("namespace Other", generated, StringComparison.Ordinal);
            Assert.Contains("partial class PlainText ", generated, StringComparison.Ordinal);
            Assert.False(File.Exists(Path.Combine(this.project, "Templates", "Plain.cs")));
        }

        [Fact]
        public void RootNamespaceAndProjectFolderCanBeGiven()
        {
            Run("generate", Path.Combine(this.project, "Templates", "Mail", "Welcome.ctt"), "--root-namespace", "Root", "--project-dir", Path.Combine(this.project, "Templates"));

            Assert.Contains("namespace Root.Mail", File.ReadAllText(Path.Combine(this.project, "Templates", "Mail", "Welcome.g.cs")), StringComparison.Ordinal);
        }

        [Fact]
        public void WriterUsesWriteToken()
        {
            Run("generate", Path.Combine(this.project, "Templates", "Mail", "Welcome.ctt"), "--writer");

            Assert.Contains("this.WriteToken(", File.ReadAllText(Path.Combine(this.project, "Templates", "Mail", "Welcome.g.cs")), StringComparison.Ordinal);
            Assert.Contains("partial void WriteToken(", File.ReadAllText(Path.Combine(this.project, "Templates", "Mail", "Welcome.cs")), StringComparison.Ordinal);

            // The next run sees WriteToken in the stub and keeps it without the switch.
            Run("generate", Path.Combine(this.project, "Templates", "Mail", "Welcome.ctt"));
            Assert.Contains("this.WriteToken(", File.ReadAllText(Path.Combine(this.project, "Templates", "Mail", "Welcome.g.cs")), StringComparison.Ordinal);
        }

        [Fact]
        public void ATemplateErrorIsShownWithItsLineAndColumn()
        {
            var bad = Path.Combine(this.project, "Templates", "Bad.ctt");
            File.WriteAllText(bad, "ok\n  <%FOREACH rows%>");

            var (code, text, errors) = Run("generate", Path.Combine(this.project, "Templates"));

            Assert.Equal(1, code);
            Assert.StartsWith(bad + "(2,3): error TTC001: ", errors, StringComparison.Ordinal);
            Assert.Contains("2 of 3 templates done.", text, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(2, "build")]
        [InlineData(2, "generate")]
        [InlineData(2, "generate", "missing.ctt")]
        [InlineData(2, "generate", "--class")]
        [InlineData(2, "generate", "--bad")]
        [InlineData(0, "--help")]
        public void WrongCommandLines(int expected, params string[] args)
        {
            Assert.Equal(expected, Run(args).Code);
        }

        [Fact]
        public void ClassNeedsOneTemplate()
        {
            Assert.Equal(2, Run("generate", Path.Combine(this.project, "Templates"), "--class", "X").Code);
        }

        [Fact]
        public void ABadClassNameIsAnError()
        {
            var (code, _, errors) = Run("generate", Path.Combine(this.project, "Templates", "Plain.ctt"), "--class", "1x");

            Assert.Equal(1, code);
            Assert.Contains("error TTC002", errors, StringComparison.Ordinal);
        }

        private static (int Code, string Output, string Errors) Run(params string[] args)
        {
            var output = new StringWriter();
            var errors = new StringWriter();
            int code = CliProgram.Run(args, output, errors);
            return (code, output.ToString(), errors.ToString());
        }
    }
}
