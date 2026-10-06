// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.CodeGen
{
    using System.Collections.Generic;
    using System.IO;

    using Toshal.Template.Tokens;

    // The templates in Templates/, embedded in this assembly. Each one is parsed once and then only processed.
    internal static class EmbeddedTemplates
    {
        public static List<IToken> Parse(string fileName)
        {
            var name = "Toshal.Template.CodeGen.Templates." + fileName;
            using var stream = typeof(EmbeddedTemplates).Assembly.GetManifestResourceStream(name)
                ?? throw new InvalidOperationException("The embedded template " + name + " is missing.");
            using var reader = new StreamReader(stream);
            return new Parser().Parse(reader.ReadToEnd());
        }
    }
}
