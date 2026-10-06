// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Generator
{
    using System.IO;
    using System.Linq;
    using System.Text;

    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    using Toshal.Template.CodeGen;
    using Toshal.Template.Exceptions;

    /// <summary>
    /// An MSBuild task that turns each <c>.ctt</c> template into C# files next to it: <c>Templates/Mail.ctt</c> gets <c>Templates/Mail.g.cs</c>,
    /// the generated half of the class, and <c>Templates/Mail.cs</c>, the other half with the partial methods to fill in. The package runs it
    /// before the compiler.
    /// </summary>
    /// <remarks>
    /// <para><c>Name.g.cs</c> is made again on every build, but written only when its text changes. Do not edit it: a change is lost on the next
    /// build. <c>Name.cs</c> is written only when it does not exist, and never changed after that. Set the property
    /// <c>ToshalTemplateCreateStubs</c> to false to turn the stubs off.</para>
    /// <para>The class name is the file name; the namespace is the root namespace and the folders, as for .resx files. The item metadata
    /// <c>ClassName</c> and <c>Namespace</c> change them. When <c>Name.cs</c> has a <c>partial void WriteToken(</c> method, value tags call it
    /// instead of <c>TokenValue</c>.</para>
    /// <para>A template the parser rejects gives the error TTC001 at its line and column; a template that gives no valid class name gives the
    /// error TTC002. Neither writes a file, and the build fails.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// &lt;GenerateTemplates Templates="@(AdditionalFiles)" RootNamespace="$(RootNamespace)" ProjectDirectory="$(MSBuildProjectDirectory)"&gt;
    ///   &lt;Output TaskParameter="GeneratedFiles" ItemName="Generated" /&gt;
    ///   &lt;Output TaskParameter="CreatedFiles" ItemName="NewStubs" /&gt;
    /// &lt;/GenerateTemplates&gt;
    /// </code>
    /// </example>
    public sealed class GenerateTemplates : Task
    {
        /// <summary>
        /// Gets or sets the template items. Items that do not end with <c>.ctt</c> are ignored. The metadata <c>ClassName</c> and <c>Namespace</c>
        /// change the names that come from the path.
        /// </summary>
        /// <example>
        /// <code>
        /// Templates="@(AdditionalFiles)"
        /// </code>
        /// </example>
        [Required]
        public ITaskItem[] Templates { get; set; } = Array.Empty<ITaskItem>();

        /// <summary>
        /// Gets or sets the root namespace of the project.
        /// </summary>
        /// <example>
        /// <code>
        /// RootNamespace="$(RootNamespace)"
        /// </code>
        /// </example>
        public string? RootNamespace { get; set; }

        /// <summary>
        /// Gets or sets the project folder. The folders between it and a template become part of the namespace.
        /// </summary>
        /// <example>
        /// <code>
        /// ProjectDirectory="$(MSBuildProjectDirectory)"
        /// </code>
        /// </example>
        [Required]
        public string ProjectDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether a missing stub (<c>Name.cs</c>) is written. The default is true.
        /// </summary>
        /// <example>
        /// <code>
        /// CreateStubs="$(ToshalTemplateCreateStubs)"
        /// </code>
        /// </example>
        public bool CreateStubs { get; set; } = true;

        /// <summary>
        /// Gets the generated files (<c>Name.g.cs</c>) of this run, written or unchanged. The package adds them to the Compile items of this build.
        /// </summary>
        /// <example>
        /// <code>
        /// &lt;Output TaskParameter="GeneratedFiles" ItemName="Generated" /&gt;
        /// </code>
        /// </example>
        [Output]
        public ITaskItem[] GeneratedFiles { get; private set; } = Array.Empty<ITaskItem>();

        /// <summary>
        /// Gets the stub files this run wrote. The package adds them to the Compile items of this build.
        /// </summary>
        /// <example>
        /// <code>
        /// &lt;Output TaskParameter="CreatedFiles" ItemName="NewStubs" /&gt;
        /// </code>
        /// </example>
        [Output]
        public ITaskItem[] CreatedFiles { get; private set; } = Array.Empty<ITaskItem>();

        /// <summary>
        /// Writes the generated files and the missing stubs.
        /// </summary>
        /// <returns>False when a template has an error; the error is logged with its file, line and column.</returns>
        /// <example>
        /// <code>
        /// // MSBuild calls it.
        /// </code>
        /// </example>
        public override bool Execute()
        {
            var generated = new List<ITaskItem>();
            var created = new List<ITaskItem>();
            foreach (var item in this.Templates)
            {
                var path = item.GetMetadata("FullPath");
                if (!path.EndsWith(TemplateNaming.Extension, StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) continue;

                var names = TemplateNaming.FromPath(this.RootNamespace, this.ProjectDirectory, path, item.GetMetadata("Namespace"), item.GetMetadata("ClassName"));
                var relative = string.Join("/", TemplateNaming.FolderNames(this.ProjectDirectory, path).Concat(new[] { Path.GetFileName(path) }));
                if (!TemplateNaming.IsValidName(names.ClassName, dotted: false) || (names.Namespace.Length > 0 && !TemplateNaming.IsValidName(names.Namespace, dotted: true)))
                {
                    this.Log.LogError(
                        null, "TTC002", null, path, 0, 0, 0, 0,
                        "The template {0} gives the class name '{1}' in the namespace '{2}', which is not valid C#. Set the ClassName or Namespace metadata of the item.",
                        relative, names.ClassName, names.Namespace);
                    continue;
                }

                var stubPath = StubFile.PathFor(path);
                var source = new TemplateSource(names.Namespace, names.ClassName, File.ReadAllText(path))
                {
                    SourcePath = relative,
                    UseTokenWriter = GeneratedFile.UsesTokenWriter(stubPath),
                };

                TemplateCode code;
                try
                {
                    code = CSharpEmitter.Emit(source);
                }
                catch (ParserException ex)
                {
                    this.Log.LogError(null, "TTC001", null, path, ex.LineNumber, ex.StartingPosition, 0, 0, "{0}: {1}", relative, ex.Message);
                    continue;
                }

                var generatedPath = GeneratedFile.PathFor(path);
                if (GeneratedFile.WriteIfChanged(generatedPath, code.Code))
                {
                    this.Log.LogMessage(MessageImportance.Normal, "Toshal.Template: wrote {0}.", generatedPath);
                }

                generated.Add(new TaskItem(generatedPath));

                if (this.CreateStubs && !File.Exists(stubPath))
                {
                    File.WriteAllText(stubPath, StubEmitter.Emit(source, code), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                    this.Log.LogMessage(MessageImportance.High, "Toshal.Template: made {0} for your values.", stubPath);
                    created.Add(new TaskItem(stubPath));
                }
            }

            this.GeneratedFiles = generated.ToArray();
            this.CreatedFiles = created.ToArray();
            return !this.Log.HasLoggedErrors;
        }
    }
}
