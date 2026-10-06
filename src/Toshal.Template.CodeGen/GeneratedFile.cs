// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.CodeGen
{
    using System.IO;
    using System.Text;

    // Shared by the MSBuild task and the tool: where the generated half of a template goes, and how it is written.
    public static class GeneratedFile
    {
        // Templates/Mail.ctt gets Templates/Mail.g.cs.
        public static string PathFor(string templatePath) => Path.ChangeExtension(templatePath, ".g.cs");

        // True when the stub (the other half of the class) has a WriteToken method, so <%=name%> calls it instead of TokenValue.
        public static bool UsesTokenWriter(string stubPath) =>
            File.Exists(stubPath) && File.ReadAllText(stubPath).IndexOf("partial void WriteToken(", StringComparison.Ordinal) >= 0;

        // Writes the file only when its text changes, so an unchanged template does not make the project build again.
        // Returns true when the file was written.
        public static bool WriteIfChanged(string path, string text)
        {
            if (File.Exists(path) && File.ReadAllText(path) == text) return false;

            File.WriteAllText(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return true;
        }
    }
}
