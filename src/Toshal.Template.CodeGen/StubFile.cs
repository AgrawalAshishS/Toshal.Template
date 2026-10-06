// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

// This code runs in MSBuild or in the tool, never in the compiler, so it may write files.
#pragma warning disable RS1035

namespace Toshal.Template.CodeGen
{
    using System.IO;

    using Toshal.Template.Exceptions;

    // Shared by the MSBuild task and the tool: where the stub of a template goes, and its text.
    public static class StubFile
    {
        // Templates/Mail.ctt gets Templates/Mail.cs.
        public static string PathFor(string templatePath) => Path.ChangeExtension(templatePath, ".cs");

        // The stub text, or null when the template has an error (the generator or the tool reports it).
        public static string? Create(string templatePath, string classNamespace, string className, string sourcePath, bool useTokenWriter = false)
        {
            var source = new TemplateSource(classNamespace, className, File.ReadAllText(templatePath)) { SourcePath = sourcePath, UseTokenWriter = useTokenWriter };
            try
            {
                return StubEmitter.Emit(source, CSharpEmitter.Emit(source));
            }
            catch (ParserException)
            {
                return null;
            }
        }
    }
}
