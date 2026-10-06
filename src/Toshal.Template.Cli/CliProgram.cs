// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Cli
{
    using System.Text;
    using System.Xml.Linq;

    using Toshal.Template.CodeGen;
    using Toshal.Template.Exceptions;

    /// <summary>
    /// The <c>toshal-template</c> tool. <c>toshal-template generate &lt;file or folder&gt;...</c> writes <c>Name.g.cs</c> next to each <c>.ctt</c>
    /// template (every time) and <c>Name.cs</c>, the other half of the class with the partial methods to fill in (only when it does not exist).
    /// </summary>
    /// <remarks>
    /// <para>The class name is the file name. The namespace is the root namespace of the nearest project file (its RootNamespace, else its
    /// name) and the folders between the project folder and the template, as for the Toshal.Template.Generator package.</para>
    /// <para><b>Warning:</b> use the tool or the Toshal.Template.Generator package in a project, not both: both would make the same class.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// toshal-template generate Templates
    /// toshal-template generate Templates/Mail.ctt --class MailTemplate --namespace Shop.Mail
    /// </code>
    /// </example>
    public static class CliProgram
    {
        private const string Usage = @"Usage: toshal-template generate <file.ctt or folder>... [options]

Writes Name.g.cs next to each .ctt template (every time) and Name.cs, the other half of the class
with the partial methods to fill in (only when Name.cs does not exist yet). A folder means every
.ctt file in it and in its sub folders.

Options:
  --root-namespace <name>  Root namespace. Default: RootNamespace of the nearest .csproj, else its name.
  --project-dir <folder>   The folders below it become part of the namespace. Default: the folder of the nearest .csproj.
  --namespace <name>       Namespace of the class, instead of root namespace + folders.
  --class <name>           Class name, instead of the file name (only with one template).
  --writer                 <%=name%> calls WriteToken(args, output) instead of TokenValue(args).
                           Also used when Name.cs already has a WriteToken method.
  --no-stub                Do not write Name.cs.
";

        /// <summary>
        /// Runs the tool.
        /// </summary>
        /// <param name="args">The command line.</param>
        /// <param name="output">Where messages go.</param>
        /// <param name="error">Where errors go.</param>
        /// <returns>0 when all went well, 1 when a template has an error, 2 for a wrong command line.</returns>
        /// <example>
        /// <code>
        /// int code = CliProgram.Run(new[] { "generate", "Templates" }, Console.Out, Console.Error);
        /// </code>
        /// </example>
        public static int Run(string[] args, TextWriter output, TextWriter error)
        {
            if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
            {
                output.Write(Usage);
                return args.Length == 0 ? 2 : 0;
            }

            if (args[0] != "generate")
            {
                error.WriteLine("Unknown command: " + args[0]);
                error.Write(Usage);
                return 2;
            }

            var options = new Options();
            var inputs = new List<string>();
            for (int i = 1; i < args.Length; i++)
            {
                string Value()
                {
                    if (i + 1 >= args.Length) throw new UsageException(args[i] + " needs a value.");
                    return args[++i];
                }

                try
                {
                    switch (args[i])
                    {
                        case "--root-namespace": options.RootNamespace = Value(); break;
                        case "--project-dir": options.ProjectDirectory = Path.GetFullPath(Value()); break;
                        case "--namespace": options.Namespace = Value(); break;
                        case "--class": options.ClassName = Value(); break;
                        case "--writer": options.Writer = true; break;
                        case "--no-stub": options.NoStub = true; break;
                        default:
                            if (args[i].StartsWith("--", StringComparison.Ordinal)) throw new UsageException("Unknown option: " + args[i]);
                            inputs.Add(args[i]);
                            break;
                    }
                }
                catch (UsageException ex)
                {
                    error.WriteLine(ex.Message);
                    return 2;
                }
            }

            var templates = new List<string>();
            foreach (var input in inputs)
            {
                var full = Path.GetFullPath(input);
                if (Directory.Exists(full))
                {
                    templates.AddRange(Directory.EnumerateFiles(full, "*" + TemplateNaming.Extension, SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal));
                }
                else if (File.Exists(full))
                {
                    templates.Add(full);
                }
                else
                {
                    error.WriteLine("Not found: " + input);
                    return 2;
                }
            }

            if (templates.Count == 0)
            {
                error.WriteLine("No .ctt templates given.");
                return 2;
            }

            if (options.ClassName != null && templates.Count > 1)
            {
                error.WriteLine("--class can be used with one template only.");
                return 2;
            }

            int failed = 0;
            foreach (var template in templates)
            {
                if (!Generate(template, options, output, error)) failed++;
            }

            output.WriteLine($"{templates.Count - failed} of {templates.Count} templates done.");
            return failed == 0 ? 0 : 1;
        }

        private static bool Generate(string templatePath, Options options, TextWriter output, TextWriter error)
        {
            var project = FindProject(templatePath);
            var projectDirectory = options.ProjectDirectory ?? (project != null ? Path.GetDirectoryName(project)! : Path.GetDirectoryName(templatePath)!);
            var rootNamespace = options.RootNamespace ?? (project != null ? RootNamespaceOf(project) : null);
            var names = TemplateNaming.FromPath(rootNamespace, projectDirectory, templatePath, options.Namespace, options.ClassName);
            var relative = string.Join("/", TemplateNaming.FolderNames(projectDirectory, templatePath).Concat(new[] { Path.GetFileName(templatePath) }));

            if (!TemplateNaming.IsValidName(names.ClassName, dotted: false) || (names.Namespace.Length > 0 && !TemplateNaming.IsValidName(names.Namespace, dotted: true)))
            {
                error.WriteLine($"{templatePath}: error TTC002: the class name '{names.ClassName}' in the namespace '{names.Namespace}' is not valid C#. Use --class or --namespace.");
                return false;
            }

            var stubPath = StubFile.PathFor(templatePath);
            var writer = options.Writer || GeneratedFile.UsesTokenWriter(stubPath);
            var source = new TemplateSource(names.Namespace, names.ClassName, File.ReadAllText(templatePath)) { SourcePath = relative, UseTokenWriter = writer };

            TemplateCode code;
            try
            {
                code = CSharpEmitter.Emit(source);
            }
            catch (ParserException ex)
            {
                error.WriteLine($"{templatePath}({ex.LineNumber},{ex.StartingPosition}): error TTC001: {ex.Message}");
                return false;
            }

            var generatedPath = GeneratedFile.PathFor(templatePath);
            if (GeneratedFile.WriteIfChanged(generatedPath, code.Code))
            {
                output.WriteLine("Wrote " + generatedPath);
            }

            if (!options.NoStub && !File.Exists(stubPath))
            {
                File.WriteAllText(stubPath, StubEmitter.Emit(source, code), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                output.WriteLine("Made " + stubPath + " for your values.");
            }

            return true;
        }

        // The nearest .csproj in the folder of the template or above it.
        private static string? FindProject(string templatePath)
        {
            for (var folder = Path.GetDirectoryName(templatePath); folder != null; folder = Path.GetDirectoryName(folder))
            {
                var project = Directory.EnumerateFiles(folder, "*.csproj").OrderBy(f => f, StringComparer.Ordinal).FirstOrDefault();
                if (project != null) return project;
            }

            return null;
        }

        private static string RootNamespaceOf(string project)
        {
            try
            {
                var value = XDocument.Load(project).Descendants().FirstOrDefault(e => e.Name.LocalName == "RootNamespace")?.Value;
                if (!string.IsNullOrWhiteSpace(value) && !value.Contains("$(", StringComparison.Ordinal)) return value.Trim();
            }
            catch (System.Xml.XmlException)
            {
                // A project file that is not valid XML: fall back to its name.
            }

            return Path.GetFileNameWithoutExtension(project);
        }

        private sealed class Options
        {
            public string? RootNamespace { get; set; }

            public string? ProjectDirectory { get; set; }

            public string? Namespace { get; set; }

            public string? ClassName { get; set; }

            public bool Writer { get; set; }

            public bool NoStub { get; set; }
        }

        private sealed class UsageException : Exception
        {
            public UsageException(string message)
                : base(message)
            {
            }
        }
    }
}
