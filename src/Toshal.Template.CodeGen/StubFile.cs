// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.CodeGen
{
    using System.IO;

    // Shared by the MSBuild task and the tool: where the stub of a template goes.
    public static class StubFile
    {
        // Templates/Mail.ctt gets Templates/Mail.cs.
        public static string PathFor(string templatePath) => Path.ChangeExtension(templatePath, ".cs");
    }
}
