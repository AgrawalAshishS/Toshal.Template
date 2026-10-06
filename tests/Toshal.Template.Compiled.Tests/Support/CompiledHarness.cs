extern alias codegen;

using System.Reflection;
using System.Runtime.Loader;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using CSharpEmitter = codegen::Toshal.Template.CodeGen.CSharpEmitter;
using ProviderKinds = codegen::Toshal.Template.CodeGen.ProviderKinds;
using TemplateSource = codegen::Toshal.Template.CodeGen.TemplateSource;

namespace Toshal.Template.Compiled.Tests.Support
{
    // Emits the C# of templates, compiles it in memory with Roslyn together with a small other half that sends every partial method to
    // TestProviders, and runs it. The same templates and providers also go through Processor, so the two texts can be compared.
    public static class CompiledHarness
    {
        public sealed record Entry(string ClassName, string Template, bool Writer = false);

        public sealed class Compiled
        {
            private readonly Assembly assembly;

            public Compiled(Assembly assembly, Dictionary<string, string> code)
            {
                this.assembly = assembly;
                this.Code = code;
            }

            // The generated half of each class, by class name.
            public Dictionary<string, string> Code { get; }

            public CompiledTemplate Create(string className, TestProviders providers)
            {
                var type = this.assembly.GetType("Generated." + className, throwOnError: true)!;
                var template = (CompiledTemplate)Activator.CreateInstance(type)!;
                type.GetProperty("Providers")!.SetValue(template, providers);
                return template;
            }

            // Runs a class; sub templates use the classes Sub_name (or SubW_name for a writer class).
            public string Run(string className, TestProviders providers, object? context, bool writer = false)
            {
                providers.CompiledSubTemplate = name => this.Create((writer ? "SubW_" : "Sub_") + name, providers);
                return this.Create(className, providers).Process(context).ToString();
            }
        }

        public static Compiled Compile(IEnumerable<Entry> entries, IReadOnlyDictionary<string, string>? subTemplates = null)
        {
            var all = entries.ToList();
            foreach (var sub in subTemplates ?? new TestProviders().SubTemplates)
            {
                all.Add(new Entry("Sub_" + sub.Key, sub.Value));
                all.Add(new Entry("SubW_" + sub.Key, sub.Value, Writer: true));
            }

            var trees = new List<SyntaxTree>();
            var code = new Dictionary<string, string>();
            foreach (var entry in all)
            {
                var emitted = CSharpEmitter.Emit(new TemplateSource("Generated", entry.ClassName, entry.Template) { UseTokenWriter = entry.Writer, SourcePath = entry.ClassName + ".ctt" });
                code[entry.ClassName] = emitted.Code;
                trees.Add(CSharpSyntaxTree.ParseText(emitted.Code, path: entry.ClassName + ".g.cs"));
                trees.Add(CSharpSyntaxTree.ParseText(OtherHalf(entry.ClassName, emitted.Kinds), path: entry.ClassName + ".cs"));
            }

            var compilation = CSharpCompilation.Create(
                "Generated" + Guid.NewGuid().ToString("N"),
                trees,
                References.Value,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable, optimizationLevel: OptimizationLevel.Release));

            using var stream = new MemoryStream();
            var result = compilation.Emit(stream);
            var problems = result.Diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning).ToList();
            if (problems.Count > 0)
            {
                var text = new StringBuilder();
                foreach (var problem in problems.Take(20))
                {
                    text.AppendLine(problem.ToString());
                    var file = problem.Location.SourceTree?.FilePath;
                    var name = file?.Replace(".g.cs", string.Empty).Replace(".cs", string.Empty);
                    if (name != null && code.TryGetValue(name, out var source) && file!.EndsWith(".g.cs", StringComparison.Ordinal))
                    {
                        text.AppendLine(source);
                    }
                }

                throw new InvalidOperationException("The generated code does not compile cleanly:\r\n" + text);
            }

            stream.Position = 0;
            return new Compiled(AssemblyLoadContext.Default.LoadFromStream(stream), code);
        }

        // Runs a template through Processor with the same providers.
        public static string RunProcessor(string template, TestProviders providers, object? context, bool writer = false)
        {
            var processor = new Processor
            {
                ConditionValueProvider = providers.Condition,
                LoopValueProvider = providers.Loop,
                WithValueProvider = providers.With,
                ProcessTemplateValueProvider = providers.ParsedSubTemplate,
            };

            if (writer) processor.TokenWriter = providers.Write;
            else processor.TokenValueProvider = providers.Token;

            return processor.Process(new ProcessorArgs(new Parser().Parse(template)) { Context = context }).ToString();
        }

        private static string OtherHalf(string className, ProviderKinds kinds)
        {
            var text = new StringBuilder();
            text.AppendLine("namespace Generated");
            text.AppendLine("{");
            text.AppendLine("    public partial class " + className);
            text.AppendLine("    {");
            text.AppendLine("        public global::Toshal.Template.Compiled.Tests.Support.TestProviders Providers { get; set; } = null!;");
            if ((kinds & ProviderKinds.TokenValue) != 0)
                text.AppendLine("        private partial string? TokenValue(global::Toshal.Template.TokenArgs args) => this.Providers.Token(args);");
            if ((kinds & ProviderKinds.WriteToken) != 0)
                text.AppendLine("        private partial void WriteToken(global::Toshal.Template.TokenArgs args, global::System.Text.StringBuilder output) => this.Providers.Write(args, output);");
            if ((kinds & ProviderKinds.Condition) != 0)
                text.AppendLine("        private partial bool Condition(global::Toshal.Template.ConditionArgs args) => this.Providers.Condition(args);");
            if ((kinds & ProviderKinds.Loop) != 0)
                text.AppendLine("        private partial global::System.Collections.IList? Loop(global::Toshal.Template.LoopArgs args) => this.Providers.Loop(args);");
            if ((kinds & ProviderKinds.With) != 0)
                text.AppendLine("        private partial object? With(global::Toshal.Template.TokenArgs args) => this.Providers.With(args);");
            if ((kinds & ProviderKinds.SubTemplate) != 0)
                text.AppendLine("        private partial global::Toshal.Template.Compiled.CompiledTemplate? SubTemplate(global::Toshal.Template.ProcessTemplateArgs args) => this.Providers.CompiledSub(args);");
            text.AppendLine("    }");
            text.AppendLine("}");
            return text.ToString();
        }

        public static IReadOnlyList<MetadataReference> MetadataReferences => References.Value;

        private static readonly Lazy<List<MetadataReference>> References = new Lazy<List<MetadataReference>>(() =>
        {
            // Not the CodeGen and Generator assemblies: they have their own copy of the parser types.
            var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                .Where(p => !Path.GetFileName(p).Equals("Toshal.Template.CodeGen.dll", StringComparison.OrdinalIgnoreCase)
                    && !Path.GetFileName(p).Equals("Toshal.Template.Generator.dll", StringComparison.OrdinalIgnoreCase))
                .ToList();
            paths.Add(typeof(Processor).Assembly.Location);
            paths.Add(typeof(CompiledTemplate).Assembly.Location);
            paths.Add(typeof(TestProviders).Assembly.Location);
            return paths.Distinct(StringComparer.OrdinalIgnoreCase).Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)).ToList();
        });
    }
}
