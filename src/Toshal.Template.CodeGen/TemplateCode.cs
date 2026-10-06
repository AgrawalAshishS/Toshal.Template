// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.CodeGen
{
    using System.Collections.Generic;

    // The partial methods a template needs, one per kind of tag that needs data.
    [Flags]
    public enum ProviderKinds
    {
        None = 0,
        TokenValue = 1,
        WriteToken = 2,
        Condition = 4,
        Loop = 8,
        With = 16,
        SubTemplate = 32,
    }

    // The kind a name in the templates of the emitters stands for: <%IF loop%> is ProviderKinds.Loop.
    internal static class ProviderKindNames
    {
        public static ProviderKinds Parse(string name) =>
            Enum.TryParse<ProviderKinds>(name, ignoreCase: true, out var kind) ? kind : throw new InvalidOperationException("No kind named " + name + ".");
    }

    // What the emitter made from one template: the generated half of the class, and the names each partial method gets, for the stub.
    public sealed class TemplateCode
    {
        public TemplateCode(string code, ProviderKinds kinds, IReadOnlyDictionary<ProviderKinds, IReadOnlyList<string>> names)
        {
            this.Code = code;
            this.Kinds = kinds;
            this.Names = names;
        }

        public string Code { get; }

        public ProviderKinds Kinds { get; }

        // The lower case names used with each kind, sorted, without repeats.
        public IReadOnlyDictionary<ProviderKinds, IReadOnlyList<string>> Names { get; }

        public IReadOnlyList<string> NamesOf(ProviderKinds kind) => this.Names.TryGetValue(kind, out var names) ? names : Array.Empty<string>();
    }

    // The input of the emitter: where the class goes and how the template is written.
    public sealed class TemplateSource
    {
        public TemplateSource(string? classNamespace, string className, string text)
        {
            this.Namespace = classNamespace ?? string.Empty;
            this.ClassName = className;
            this.Text = text;
        }

        // Empty for the global namespace.
        public string Namespace { get; }

        public string ClassName { get; }

        public string Text { get; }

        // Shown in the header of the generated file, for example Templates/Hello.ctt.
        public string? SourcePath { get; set; }

        // True when <%=name%> calls WriteToken(args, output) instead of TokenValue(args).
        public bool UseTokenWriter { get; set; }
    }
}
