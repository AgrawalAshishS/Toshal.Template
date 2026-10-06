// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.CodeGen
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using Toshal.Template.Tokens;

    // Writes the other half of the class: the partial methods the template needs, with a case for every name it uses.
    // It is written once, next to the template, and then it belongs to the user; it is never written again.
    // The text is in Templates/Stub.rtt and Templates/StubBody.rtt; this class only gives the values.
    public static class StubEmitter
    {
        // The order of the methods in the stub.
        private static readonly ProviderKinds[] Methods =
        {
            ProviderKinds.TokenValue, ProviderKinds.WriteToken, ProviderKinds.Condition, ProviderKinds.Loop, ProviderKinds.With, ProviderKinds.SubTemplate,
        };

        private static readonly List<IToken> Stub = EmbeddedTemplates.Parse("Stub.rtt");
        private static readonly List<IToken> Body = EmbeddedTemplates.Parse("StubBody.rtt");

        public static string Emit(TemplateSource source, TemplateCode code)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (code == null) throw new ArgumentNullException(nameof(code));

            bool Has(ProviderKinds kind) => (code.Kinds & kind) != 0;

            var processor = new Processor
            {
                TokenValueProvider = args => args.Name switch
                {
                    "sourcepath" => source.SourcePath ?? "the template",
                    "namespace" => source.Namespace,
                    "classname" => source.ClassName,
                    "literal" => CodeWriter.Literal((string)args.Context!),

                    // A tag of the template syntax inside a comment, which the template cannot hold as text: <%=tag of="IF name"%>.
                    "tag" => "<%" + args.GetAttribute("of", string.Empty) + "%>",
                    _ => null,
                },

                // At the top: which kinds the template uses (hasloop, hasany). Inside FOREACH methods: which kind the row is (loop).
                ConditionValueProvider = args => args.Name switch
                {
                    "namespace" => source.Namespace.Length > 0,
                    "hasany" => code.Kinds != ProviderKinds.None,
                    "systemusings" => Has(ProviderKinds.Loop) || Has(ProviderKinds.WriteToken),
                    _ when args.Name.StartsWith("has", StringComparison.Ordinal) => Has(ProviderKindNames.Parse(args.Name.Substring(3))),
                    _ => args.Context is ProviderKinds row && row == ProviderKindNames.Parse(args.Name),
                },
                LoopValueProvider = args => args.Name switch
                {
                    "methods" => Methods.Where(Has).ToList(),
                    "names" => code.NamesOf((ProviderKinds)args.Context!).ToList(),
                    _ => (IList?)null,
                },
                ProcessTemplateValueProvider = args => args.Name == "body" ? Body : null,
            };

            return processor.Process(new ProcessorArgs(Stub)).ToString();
        }
    }
}
