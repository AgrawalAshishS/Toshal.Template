// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Generator
{
    using System.Collections.Immutable;
    using System.Linq;
    using System.Text;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Text;

    using Toshal.Template.CodeGen;
    using Toshal.Template.Exceptions;

    /// <summary>
    /// Makes a C# class from every <c>.ctt</c> file that is an AdditionalFiles item of the project. The package adds every <c>.ctt</c> file of the
    /// project folder and its sub folders, unless the property <c>ToshalTemplateAutoInclude</c> is false.
    /// </summary>
    /// <remarks>
    /// <para>The class name is the file name; the namespace is the root namespace and the folders, as for .resx files. The item metadata
    /// <c>ClassName</c> and <c>Namespace</c> change them. When the other half of the class has an implementation of <c>WriteToken</c>, value tags
    /// call it instead of <c>TokenValue</c>.</para>
    /// <para>A template the parser rejects gives the error TTC001 at its line and column.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// &lt;ItemGroup&gt;
    ///   &lt;AdditionalFiles Update="Templates\Mail.ctt" ClassName="MailTemplate" Namespace="Shop.Mail" /&gt;
    /// &lt;/ItemGroup&gt;
    /// </code>
    /// </example>
    [Generator(LanguageNames.CSharp)]
    public sealed class TemplateGenerator : IIncrementalGenerator
    {
        private static readonly DiagnosticDescriptor ParseError = new DiagnosticDescriptor(
            "TTC001", "The template has an error", "{0}", "Toshal.Template", DiagnosticSeverity.Error, isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor BadName = new DiagnosticDescriptor(
            "TTC002", "The template gives no valid class name", "The template {0} gives the class name '{1}' in the namespace '{2}', which is not valid C#. Set the ClassName or Namespace metadata of the item.",
            "Toshal.Template", DiagnosticSeverity.Error, isEnabledByDefault: true);

        /// <summary>
        /// Sets up the generator. The compiler calls it.
        /// </summary>
        /// <param name="context">The context the compiler gives.</param>
        /// <example>
        /// <code>
        /// // The compiler calls it; you do not.
        /// </code>
        /// </example>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var templates = context.AdditionalTextsProvider
                .Where(file => file.Path.EndsWith(TemplateNaming.Extension, StringComparison.OrdinalIgnoreCase))
                .Combine(context.AnalyzerConfigOptionsProvider)
                .Select((pair, cancel) => Read(pair.Left, pair.Right, cancel));

            // Classes whose other half implements WriteToken.
            var writers = context.SyntaxProvider
                .CreateSyntaxProvider(
                    (node, _) => node is MethodDeclarationSyntax method && method.Identifier.ValueText == "WriteToken" && (method.Body != null || method.ExpressionBody != null),
                    (syntax, _) => FullName((MethodDeclarationSyntax)syntax.Node))
                .Where(name => name != null)
                .Collect();

            context.RegisterSourceOutput(templates.Combine(writers), (output, pair) => Write(output, pair.Left, pair.Right!));
        }

        private static TemplateInput Read(AdditionalText file, AnalyzerConfigOptionsProvider options, CancellationToken cancel)
        {
            var global = options.GlobalOptions;
            global.TryGetValue("build_property.RootNamespace", out var rootNamespace);
            global.TryGetValue("build_property.MSBuildProjectDirectory", out var projectDirectory);

            var item = options.GetOptions(file);
            item.TryGetValue("build_metadata.AdditionalFiles.ClassName", out var className);
            item.TryGetValue("build_metadata.AdditionalFiles.Namespace", out var classNamespace);

            var names = TemplateNaming.FromPath(rootNamespace, projectDirectory ?? string.Empty, file.Path, classNamespace, className);
            var text = file.GetText(cancel)?.ToString() ?? string.Empty;
            var relative = RelativePath(projectDirectory, file.Path);
            return new TemplateInput(file.Path, relative, text, names.Namespace, names.ClassName);
        }

        private static void Write(SourceProductionContext output, TemplateInput input, ImmutableArray<string?> writers)
        {
            if (!TemplateNaming.IsValidName(input.ClassName, dotted: false) || (input.Namespace.Length > 0 && !TemplateNaming.IsValidName(input.Namespace, dotted: true)))
            {
                output.ReportDiagnostic(Diagnostic.Create(BadName, Location.Create(input.Path, default, default), input.RelativePath, input.ClassName, input.Namespace));
                return;
            }

            var fullName = input.Namespace.Length > 0 ? input.Namespace + "." + input.ClassName : input.ClassName;
            var source = new TemplateSource(input.Namespace, input.ClassName, input.Text)
            {
                SourcePath = input.RelativePath,
                UseTokenWriter = writers.Contains(fullName),
            };

            TemplateCode code;
            try
            {
                code = CSharpEmitter.Emit(source);
            }
            catch (ParserException ex)
            {
                var start = new LinePosition(Math.Max(ex.LineNumber - 1, 0), Math.Max(ex.StartingPosition - 1, 0));
                var location = Location.Create(input.Path, default, new LinePositionSpan(start, start));
                output.ReportDiagnostic(Diagnostic.Create(ParseError, location, input.RelativePath + ": " + ex.Message));
                return;
            }

            output.AddSource(HintName(input.RelativePath), SourceText.From(code.Code, Encoding.UTF8));
        }

        // A file name for the generated source that is unique per template: its path with other chars as '_'.
        private static string HintName(string relativePath)
        {
            var sb = new StringBuilder(relativePath.Length + 5);
            foreach (var c in relativePath)
            {
                sb.Append(char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-' ? c : '_');
            }

            return sb.Append(".g.cs").ToString();
        }

        private static string RelativePath(string? projectDirectory, string path)
        {
            var folders = TemplateNaming.FolderNames(projectDirectory ?? string.Empty, path).ToList();
            folders.Add(System.IO.Path.GetFileName(path));
            return string.Join("/", folders);
        }

        // The full name of the class around a method, from the syntax only: Namespace.Outer.Class.
        private static string? FullName(MethodDeclarationSyntax method)
        {
            if (method.Parent is not TypeDeclarationSyntax type) return null;

            var parts = new List<string>();
            for (SyntaxNode? node = type; node != null; node = node.Parent)
            {
                switch (node)
                {
                    case TypeDeclarationSyntax t:
                        parts.Insert(0, t.Identifier.ValueText);
                        break;
                    case BaseNamespaceDeclarationSyntax n:
                        parts.Insert(0, n.Name.ToString());
                        break;
                }
            }

            return string.Join(".", parts);
        }

        // What one template needs, as plain strings, so the compiler can cache the step when nothing changed.
        private sealed record TemplateInput(string Path, string RelativePath, string Text, string Namespace, string ClassName);
    }
}
