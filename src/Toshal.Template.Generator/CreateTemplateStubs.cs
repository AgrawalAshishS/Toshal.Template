// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

// This code runs in MSBuild or in the tool, never in the compiler, so it may write files.
#pragma warning disable RS1035

namespace Toshal.Template.Generator
{
    using System.IO;
    using System.Linq;
    using System.Text;

    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    using Toshal.Template.CodeGen;

    /// <summary>
    /// An MSBuild task that writes the other half of the class of each <c>.ctt</c> template, next to it, when that file does not exist yet:
    /// <c>Templates/Mail.ctt</c> gets <c>Templates/Mail.cs</c> with the partial methods to fill in. A file that exists is never changed.
    /// The package runs it before the compiler; set the property <c>ToshalTemplateCreateStubs</c> to false to turn it off.
    /// </summary>
    /// <remarks>
    /// <para>Source generators may not write files, so this task does it. A template with an error gets no stub; the generator reports the error.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// &lt;CreateTemplateStubs Templates="@(AdditionalFiles)" RootNamespace="$(RootNamespace)" ProjectDirectory="$(MSBuildProjectDirectory)"&gt;
    ///   &lt;Output TaskParameter="CreatedFiles" ItemName="NewStubs" /&gt;
    /// &lt;/CreateTemplateStubs&gt;
    /// </code>
    /// </example>
    public sealed class CreateTemplateStubs : Task
    {
        /// <summary>
        /// Gets or sets the template items. Items that do not end with <c>.ctt</c> are ignored. The metadata <c>ClassName</c> and <c>Namespace</c>
        /// are used as by the generator.
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
        /// Writes the missing stubs.
        /// </summary>
        /// <returns>True; a template with an error is skipped, not a failure of this task.</returns>
        /// <example>
        /// <code>
        /// // MSBuild calls it.
        /// </code>
        /// </example>
        public override bool Execute()
        {
            var created = new List<ITaskItem>();
            foreach (var item in this.Templates)
            {
                var path = item.GetMetadata("FullPath");
                if (!path.EndsWith(TemplateNaming.Extension, StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) continue;

                var stubPath = StubFile.PathFor(path);
                if (File.Exists(stubPath)) continue;

                var names = TemplateNaming.FromPath(this.RootNamespace, this.ProjectDirectory, path, item.GetMetadata("Namespace"), item.GetMetadata("ClassName"));
                var stub = StubFile.Create(path, names.Namespace, names.ClassName, Path.GetFileName(path));
                if (stub == null) continue;

                File.WriteAllText(stubPath, stub, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                this.Log.LogMessage(MessageImportance.High, "Toshal.Template: made {0} for your values.", stubPath);
                created.Add(new TaskItem(stubPath));
            }

            this.CreatedFiles = created.ToArray();
            return true;
        }
    }
}
