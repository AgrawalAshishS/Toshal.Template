// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.CodeGen
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Toshal.Template.Tokens;

    // Turns a template into the generated half of a class that derives from Toshal.Template.Compiled.CompiledTemplate.
    // Each block of tokens becomes the same steps Processor takes for it, written out as C#: text becomes Append calls, IF becomes if,
    // FOREACH becomes a method with a for loop. The run state (parent stack, SET scopes, FOREACH rows) is the one Processor uses, so the text is the same.
    public static class CSharpEmitter
    {
        private const string Sb = "global::System.Text.StringBuilder";
        private const string Run = "global::Toshal.Template.Compiled.TemplateRun";
        private const string Scope = "global::Toshal.Template.Compiled.TemplateScope";
        private const string Attrs = "global::Toshal.Template.Tokens.TokenAttributeDictionary";

        private static readonly List<IToken> Generated = EmbeddedTemplates.Parse("Generated.rtt");
        private static readonly List<IToken> GeneratedClass = EmbeddedTemplates.Parse("GeneratedClass.rtt");

        // Parses the template and writes the generated half. Throws the ParserException of the parser for a wrong template.
        public static TemplateCode Emit(TemplateSource source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var tokens = new Parser().Parse(source.Text);
            return new Emitter(source).Emit(tokens);
        }

        private sealed class Emitter
        {
            private readonly TemplateSource source;
            private readonly CodeWriter body;
            private readonly Dictionary<string, string> attributeFields = new Dictionary<string, string>(StringComparer.Ordinal);
            private readonly List<string> attributeLines = new List<string>();
            private readonly Dictionary<ForEachToken, int> forEachIds = new Dictionary<ForEachToken, int>(ReferenceEqualityComparer.Instance);
            private readonly List<ForEachToken> forEachToWrite = new List<ForEachToken>();
            private readonly Dictionary<ProviderKinds, SortedSet<string>> names = new Dictionary<ProviderKinds, SortedSet<string>>();
            private readonly HashSet<string> notNull = new HashSet<string>(StringComparer.Ordinal) { "list" };
            private ProviderKinds kinds;
            private int next;

            public Emitter(TemplateSource source)
            {
                this.source = source;

                // The body sits inside the class; the namespace adds its indent when the file is put together.
                this.body = new CodeWriter(1);
            }

            public TemplateCode Emit(List<IToken> tokens)
            {
                var w = this.body;

                w.Open("protected override void Render(" + Sb + " output, object? context, " + Run + " run, ref " + Scope + " scope)");
                this.Block(tokens, "output", "context", "scope");
                w.Close();

                // FOREACH methods; a FOREACH found while writing one is added to the list and written after it.
                for (int i = 0; i < this.forEachToWrite.Count; i++)
                {
                    w.Line();
                    this.ForEachMethod(this.forEachToWrite[i]);
                }

                var code = this.Assemble();
                var namesByKind = this.names.ToDictionary(p => p.Key, p => (IReadOnlyList<string>)p.Value.ToList());
                return new TemplateCode(code, this.kinds, namesByKind);
            }

            // The file around the body: the text is in Templates/Generated.rtt and Templates/GeneratedClass.rtt. Inside a namespace,
            // Processor indents the class with the line of its PROCESS_TEMPLATE tag.
            private string Assemble()
            {
                var source = this.source;
                var kinds = this.kinds;
                var body = this.body.ToString();
                var processor = new Processor
                {
                    TokenValueProvider = args => args.Name switch
                    {
                        "sourcepath" => source.SourcePath ?? "a template",
                        "namespace" => source.Namespace,
                        "classname" => source.ClassName,

                        // Without its last line break: the line of the tag has one.
                        "body" => body.Substring(0, body.Length - 2),
                        _ => null,
                    },
                    ConditionValueProvider = args => args.Name switch
                    {
                        "namespace" => source.Namespace.Length > 0,
                        "hasany" => kinds != ProviderKinds.None,
                        _ => (kinds & ProviderKindNames.Parse(args.Name)) != 0,
                    },
                    LoopValueProvider = args => args.Name == "attributes" ? this.attributeLines : null,
                    ProcessTemplateValueProvider = args => args.Name == "class" ? GeneratedClass : null,
                };

                return processor.Process(new ProcessorArgs(Generated)).ToString();
            }

            private void Use(ProviderKinds kind, string name)
            {
                this.kinds |= kind;
                if (!this.names.TryGetValue(kind, out var set))
                {
                    this.names[kind] = set = new SortedSet<string>(StringComparer.Ordinal);
                }

                set.Add(name);
            }

            private string NewName(string prefix) => prefix + (this.next++).ToString(System.Globalization.CultureInfo.InvariantCulture);

            // One static field per distinct set of attributes, made once when the class is first used.
            private string AttributesField(TokenAttributeDictionary attributes)
            {
                var pairs = attributes.OrderBy(p => p.Key, StringComparer.Ordinal).ToList();
                var key = string.Join("\0", pairs.Select(p => p.Key + "\0" + p.Value));
                if (this.attributeFields.TryGetValue(key, out var field)) return field;

                field = "A" + this.attributeFields.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
                this.attributeFields[key] = field;
                var args = string.Join(", ", pairs.Select(p => CodeWriter.Literal(p.Key) + ", " + CodeWriter.Literal(p.Value)));
                this.attributeLines.Add("private static readonly " + Attrs + " " + field + " = Attributes(" + args + ");");
                return field;
            }

            private int ForEachId(ForEachToken token)
            {
                if (!this.forEachIds.TryGetValue(token, out var id))
                {
                    id = this.forEachIds.Count;
                    this.forEachIds[token] = id;
                    this.forEachToWrite.Add(token);
                }

                return id;
            }

            // Writes the steps for one list of tokens. output, context and scope are the names of the C# variables to use.
            // Template text is held back until a tag that writes or reads the output comes, so that REMOVE_PREVIOUS and REMOVE_PREVIOUS_NEW_LINE
            // can remove it at build time and the generated code writes the shorter text.
            private void Block(List<IToken> tokens, string output, string context, string scope)
            {
                var w = this.body;
                var text = new StringBuilder();

                void Flush()
                {
                    if (text.Length == 0) return;
                    w.Line(output + ".Append(" + CodeWriter.Literal(text.ToString()) + ");");
                    text.Clear();
                }

                foreach (var token in tokens)
                {
                    if (token is not ContentToken && token is not RemovePreviousCharsToken && token is not RemovePreviousNewLineToken)
                    {
                        Flush();
                    }

                    switch (token)
                    {
                        case ContentToken contentToken:
                            text.Append(contentToken.Content);
                            break;

                        case NamedToken namedToken:
                            this.Named(namedToken, output, context, scope);
                            break;

                        case ConditionToken conditionToken:
                            this.Condition(conditionToken, "if", output, context, scope);
                            break;

                        case ForEachToken forEachToken:
                            this.Use(ProviderKinds.Loop, forEachToken.Name);
                            w.Line("this.ForEach" + this.ForEachId(forEachToken) + "(" + output + ", " + context + ", run.LoopArgs(" + CodeWriter.Literal(forEachToken.Name) + ", "
                                + this.AttributesField(forEachToken.Attributes) + ", " + context + "), run, ref " + scope + ");");
                            break;

                        case ReuseForEachToken reuseToken:
                            // Parser.Parse links every REUSE_FOREACH to its FOREACH.
                            var existing = reuseToken.ExistingForEachToken!;
                            this.Use(ProviderKinds.Loop, reuseToken.Name);
                            w.Line("this.ForEach" + this.ForEachId(existing) + "(" + output + ", " + context + ", run.LoopArgs(" + CodeWriter.Literal(reuseToken.Name) + ", "
                                + this.AttributesField(existing.Attributes) + ", " + context + "), run, ref " + scope + ");");
                            break;

                        case WithToken withToken:
                            this.With(withToken, output, context, scope);
                            break;

                        case RemovePreviousNewLineToken:
                            if (!RemoveNewLine(text))
                            {
                                Flush();
                                w.Line("run.RemovePreviousNewLine(" + output + ");");
                            }

                            break;

                        case RemovePreviousCharsToken removeToken:
                            // The held back text goes first; only what is left must be removed from text written at run time.
                            int fromText = Math.Min(removeToken.CharCount, text.Length);
                            text.Length -= fromText;
                            int rest = removeToken.CharCount - fromText;
                            if (rest > 0)
                            {
                                Flush();
                                w.Line("run.RemovePrevious(" + output + ", " + rest.ToString(System.Globalization.CultureInfo.InvariantCulture) + ");");
                            }

                            break;

                        case ProcessTemplateToken processTemplateToken:
                            this.Use(ProviderKinds.SubTemplate, processTemplateToken.Name);
                            w.Line("ProcessSubTemplate(this.SubTemplate(run.ProcessTemplateArgs(" + CodeWriter.Literal(processTemplateToken.Name) + ", "
                                + this.AttributesField(processTemplateToken.Attributes) + ", " + context + ")), " + output + ", " + context + ", run, ref " + scope + ", "
                                + CodeWriter.Literal(processTemplateToken.Indent) + ");");
                            break;

                        case ContextAsStringToken:
                            // The FOREACH list and a WITH object are never null where they are the context; a check would make the compiler
                            // think they may be null after it.
                            w.Line(this.notNull.Contains(context)
                                ? output + ".Append(" + context + ".ToString());"
                                : "if (" + context + " != null) " + output + ".Append(" + context + ".ToString());");
                            break;

                        case SeparatorToken separatorToken:
                            if (separatorToken.InnerTokens.Count > 0)
                            {
                                w.Open("if (run.InRowBeforeTheLast)");
                                this.Block(separatorToken.InnerTokens, output, context, scope);
                                w.Close();
                            }

                            break;

                        case SetToken setToken:
                            var value = this.NewName("value");
                            w.Line("var " + value + " = run.RentBuilder();");
                            this.Block(setToken.InnerTokens, value, context, scope);
                            w.Line(scope + ".Set(" + CodeWriter.Literal(setToken.Name) + ", " + value + ".ToString());");
                            w.Line("run.ReturnBuilder(" + value + ");");
                            break;

                        default:
                            throw new NotSupportedException("The token " + token.GetType().Name + " is not supported by compiled templates.");
                    }
                }

                Flush();
            }

            // Does REMOVE_PREVIOUS_NEW_LINE on held back template text: a '\n' at the end is removed, then a '\r' at the end. Returns false when
            // the result depends on text written at run time (the text is empty, or it is only "\n" and a '\r' written before it would go too);
            // then the text is not changed and the run time does it.
            private static bool RemoveNewLine(StringBuilder text)
            {
                if (text.Length == 0) return false;

                int end = text.Length;
                if (text[end - 1] == '\n')
                {
                    if (end == 1) return false;
                    end--;
                }

                if (text[end - 1] == '\r') end--;
                text.Length = end;
                return true;
            }

            private void Named(NamedToken token, string output, string context, string scope)
            {
                var w = this.body;
                var args = "run.TokenArgs(" + CodeWriter.Literal(token.Name) + ", " + this.AttributesField(token.Attributes) + ", " + context + ")";

                // A SET variable with the name wins over the provider.
                if (this.source.UseTokenWriter)
                {
                    this.Use(ProviderKinds.WriteToken, token.Name);
                    w.Line("if (!" + scope + ".TryAppend(" + output + ", " + CodeWriter.Literal(token.Name) + ")) this.WriteToken(" + args + ", " + output + ");");
                    return;
                }

                this.Use(ProviderKinds.TokenValue, token.Name);
                var value = this.NewName("value");
                w.Open("if (!" + scope + ".TryAppend(" + output + ", " + CodeWriter.Literal(token.Name) + "))");
                w.Line("var " + value + " = this.TokenValue(" + args + ");");
                w.Line("if (!string.IsNullOrEmpty(" + value + ")) " + output + ".Append(" + value + ");");
                w.Close();
            }

            // IF and ELSEIF share the scope of the block around them, as in Processor.
            private void Condition(ConditionToken token, string keyword, string output, string context, string scope)
            {
                var w = this.body;
                this.Use(ProviderKinds.Condition, token.Name);
                var call = "this.Condition(run.ConditionArgs(" + CodeWriter.Literal(token.Name) + ", " + this.AttributesField(token.Attributes) + ", " + context + "))";
                w.Open(keyword + " (" + (token.IsPositive ? string.Empty : "!") + call + ")");
                this.Block(token.InnerTokens, output, context, scope);
                w.Close();

                if (token.FalsePart is ElseToken elseToken)
                {
                    w.Open("else");
                    this.Block(elseToken.InnerTokens, output, context, scope);
                    w.Close();
                }
                else if (token.FalsePart is ConditionToken elseIfToken)
                {
                    this.Condition(elseIfToken, "else if", output, context, scope);
                }
            }

            private void With(WithToken token, string output, string context, string scope)
            {
                var w = this.body;
                this.Use(ProviderKinds.With, token.Name);
                var value = this.NewName("with");
                this.notNull.Add(value);
                var withScope = this.NewName("scope");
                w.Line("var " + value + " = this.With(run.TokenArgs(" + CodeWriter.Literal(token.Name) + ", " + this.AttributesField(token.Attributes) + ", " + context + "));");
                w.Open("if (" + value + " != null)");
                w.Line("run.ParentContext.Add(" + value + ");");
                w.Line("var " + withScope + " = " + Scope + ".Child(" + scope + ");");
                this.Block(token.InnerTokens, output, value, withScope);
                w.Line("run.RemoveParent(" + value + ");");
                w.Close();
            }

            // One method per FOREACH, so REUSE_FOREACH can call it again. The steps are those of Processor.ProcessForEach.
            private void ForEachMethod(ForEachToken token)
            {
                var w = this.body;
                w.Open("private void ForEach" + this.forEachIds[token] + "(" + Sb + " output, object? context, global::Toshal.Template.LoopArgs args, " + Run + " run, ref " + Scope + " scope)");
                w.Line("var list = this.Loop(args);");
                w.Open("if (list == null || list.Count == 0)");
                if (token.NoRecordTokens.Count > 0)
                {
                    w.Line("var noRecordScope = " + Scope + ".Child(scope);");
                    w.Line("run.EnterLoop();");
                    this.Block(token.NoRecordTokens, "output", "context", "noRecordScope");
                    w.Line("run.ExitLoop();");
                }

                w.Line("return;");
                w.Close();
                w.Line();
                w.Line("run.EnterLoop();");

                bool hasRowParts = new[]
                {
                    token.BeforeFirstRowTokens, token.FirstRowTokens, token.AfterFirstRowTokens,
                    token.BeforeRowTokens, token.RowTokens, token.AfterRowTokens,
                    token.BeforeAltRowTokens, token.AltRowTokens, token.AfterAltRowTokens,
                    token.BeforeLastRowTokens, token.LastRowTokens, token.AfterLastRowTokens,
                }.Any(part => part.Count > 0);

                // The rows share one scope: a SET in a row is seen by the next rows, but not after the loop.
                if (hasRowParts) w.Line("var rowScope = " + Scope + ".Child(scope);");
                w.Line("run.ParentContext.Add(list);");

                if (token.HeaderTokens.Count > 0)
                {
                    w.Line("var headerScope = " + Scope + ".Child(scope);");
                    this.Block(token.HeaderTokens, "output", "list", "headerScope");
                }

                w.Open("for (var i = 0; i < list.Count; i++)");
                w.Line("var row = list[i];");
                w.Line("run.ParentContext.Add(row);");
                w.Line("run.SetRow(isLast: i == list.Count - 1);");
                this.RowPart(token.BeforeFirstRowTokens, token.BeforeRowTokens, token.BeforeAltRowTokens, token.BeforeLastRowTokens, "beforeScope");
                this.RowPart(token.FirstRowTokens, token.RowTokens, token.AltRowTokens, token.LastRowTokens, null);
                this.RowPart(token.AfterFirstRowTokens, token.AfterRowTokens, token.AfterAltRowTokens, token.AfterLastRowTokens, "afterScope");
                w.Line("run.RemoveParent(row);");
                w.Close();
                w.Line();
                w.Line("run.LeaveRow();");

                if (token.FooterTokens.Count > 0)
                {
                    w.Line("var footerScope = " + Scope + ".Child(scope);");
                    this.Block(token.FooterTokens, "output", "list", "footerScope");
                }

                w.Line("run.RemoveParent(list);");
                w.Line("run.ExitLoop();");
                w.Close();
            }

            // Picks the part of one row the same way as Processor: the FIRST part for the first row of a list with more than one row, the LAST part
            // for the last row (so a list with one row uses LAST), the ALT part for odd rows, else the normal part. A part that is empty is never picked.
            // ownScope: the name of the child scope a BEFORE or AFTER part gets; null for the row part, which writes in the shared row scope.
            private void RowPart(List<IToken> first, List<IToken> normal, List<IToken> alt, List<IToken> last, string? ownScope)
            {
                var branches = new List<(string Condition, List<IToken> Tokens)>();
                if (first.Count > 0) branches.Add(("i == 0 && list.Count > 1", first));
                if (last.Count > 0) branches.Add(("i == list.Count - 1", last));
                if (alt.Count > 0) branches.Add(("i % 2 == 1", alt));

                var w = this.body;
                for (int b = 0; b < branches.Count; b++)
                {
                    w.Open((b == 0 ? "if (" : "else if (") + branches[b].Condition + ")");
                    this.RowPartBody(branches[b].Tokens, ownScope);
                    w.Close();
                }

                if (normal.Count > 0)
                {
                    if (branches.Count > 0)
                    {
                        w.Open("else");
                        this.RowPartBody(normal, ownScope);
                        w.Close();
                    }
                    else
                    {
                        this.RowPartBody(normal, ownScope);
                    }
                }
            }

            private void RowPartBody(List<IToken> tokens, string? ownScope)
            {
                if (ownScope == null)
                {
                    this.Block(tokens, "output", "row", "rowScope");
                    return;
                }

                var scope = this.NewName(ownScope);
                this.body.Line("var " + scope + " = " + Scope + ".Child(rowScope);");
                this.Block(tokens, "output", "row", scope);
            }
        }
    }
}
