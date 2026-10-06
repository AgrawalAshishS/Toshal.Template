// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.CodeGen
{
    using System.Collections.Generic;

    // Writes the other half of the class: the partial methods the template needs, with a case for every name it uses.
    // It is written once, next to the template, and then it belongs to the user; it is never written again.
    public static class StubEmitter
    {
        public static string Emit(TemplateSource source, TemplateCode code)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (code == null) throw new ArgumentNullException(nameof(code));

            bool hasNamespace = source.Namespace.Length > 0;
            var w = new CodeWriter();
            w.Line("// Made once by Toshal.Template from " + (source.SourcePath ?? "the template") + ". This file is yours: give the values here.");
            w.Line("// It is not made again. When the template uses a new kind of tag, the compiler names the method to add.");
            w.Line();
            w.Line("#nullable enable");
            w.Line();
            if (hasNamespace) w.Open("namespace " + source.Namespace);

            // Only the usings the methods below need, so the file has no unused ones.
            bool systemUsings = false;
            if ((code.Kinds & ProviderKinds.Loop) != 0) { w.Line("using System.Collections;"); systemUsings = true; }
            if ((code.Kinds & ProviderKinds.WriteToken) != 0) { w.Line("using System.Text;"); systemUsings = true; }
            if (systemUsings) w.Line();
            if (code.Kinds != ProviderKinds.None)
            {
                w.Line("using Toshal.Template;");
                if ((code.Kinds & ProviderKinds.SubTemplate) != 0) w.Line("using Toshal.Template.Compiled;");
                w.Line();
            }
            w.Open("public partial class " + source.ClassName);

            bool first = true;
            void Separate()
            {
                if (!first) w.Line();
                first = false;
            }

            if ((code.Kinds & ProviderKinds.TokenValue) != 0)
            {
                Separate();
                w.Line("// The text of <%=name%>. Null or empty writes nothing.");
                Switch(w, "private partial string? TokenValue(TokenArgs args)", code.NamesOf(ProviderKinds.TokenValue), "null");
            }

            if ((code.Kinds & ProviderKinds.WriteToken) != 0)
            {
                Separate();
                w.Line("// Appends the value of <%=name%> to the output. Only append; do not change text that is already there.");
                w.Open("private partial void WriteToken(TokenArgs args, StringBuilder output)");
                w.Open("switch (args.Name)");
                foreach (var name in code.NamesOf(ProviderKinds.WriteToken))
                {
                    w.Line("case " + CodeWriter.Literal(name) + ":");
                    w.Line("    // output.Append(...);");
                    w.Line("    break;");
                }

                w.Close();
                w.Close();
            }

            if ((code.Kinds & ProviderKinds.Condition) != 0)
            {
                Separate();
                w.Line("// Whether <%IF name%> or <%ELSEIF name%> is true. For <%IF not name%> you get the name without not.");
                Switch(w, "private partial bool Condition(ConditionArgs args)", code.NamesOf(ProviderKinds.Condition), "false");
            }

            if ((code.Kinds & ProviderKinds.Loop) != 0)
            {
                Separate();
                w.Line("// The rows of <%FOREACH name%>. Null or an empty list writes the NORECORD part.");
                Switch(w, "private partial IList? Loop(LoopArgs args)", code.NamesOf(ProviderKinds.Loop), "null");
            }

            if ((code.Kinds & ProviderKinds.With) != 0)
            {
                Separate();
                w.Line("// The context inside <%WITH name%>. Null skips the block.");
                Switch(w, "private partial object? With(TokenArgs args)", code.NamesOf(ProviderKinds.With), "null");
            }

            if ((code.Kinds & ProviderKinds.SubTemplate) != 0)
            {
                Separate();
                w.Line("// The template written by <%PROCESS_TEMPLATE name%>, for example new Footer(). Null writes nothing.");
                Switch(w, "private partial CompiledTemplate? SubTemplate(ProcessTemplateArgs args)", code.NamesOf(ProviderKinds.SubTemplate), "null");
            }

            w.Close();
            if (hasNamespace) w.Close();
            return w.ToString();
        }

        private static void Switch(CodeWriter w, string signature, IReadOnlyList<string> names, string defaultValue)
        {
            w.Line(signature + " => args.Name switch");
            w.Open();
            foreach (var name in names)
            {
                w.Line(CodeWriter.Literal(name) + " => " + defaultValue + ", // TODO");
            }

            w.Line("_ => " + defaultValue + ",");
            w.Close("};");
        }
    }
}
