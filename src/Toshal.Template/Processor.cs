// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Text;

    using Toshal.Template.Tokens;

    /// <summary>
    /// Turns parsed tokens into text. The data comes from five provider delegates that you set: one for each kind of tag that needs data.
    /// The processor does not read properties by reflection; your providers decide what each name means.
    /// </summary>
    /// <remarks>
    /// <para><b>Warning:</b> when a provider is not set, its tags are skipped without an error. An IF block (with its ELSE) is skipped when
    /// <see cref="ConditionValueProvider"/> is null, a FOREACH block (with its NORECORD) when <see cref="LoopValueProvider"/> is null, and so on.
    /// A <c>&lt;%=name%&gt;</c> still writes a SET variable of that name when there is one.</para>
    /// <para>The processor keeps no state between calls other than the provider properties, so one instance can process many templates.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var processor = new Processor
    /// {
    ///     TokenValueProvider = args =&gt; args.Name == "name" ? ((Customer)args.Context!).Name : null,
    ///     ConditionValueProvider = args =&gt; args.Name == "vip" &amp;&amp; ((Customer)args.Context!).IsVip,
    /// };
    /// string text = processor.Process(new ProcessorArgs(tokens) { Context = customer }).ToString();
    /// </code>
    /// </example>
    public class Processor
    {
        /// <summary>
        /// Processes the tokens with the given top context and returns the text.
        /// </summary>
        /// <param name="args">The tokens and the top context.</param>
        /// <returns>A new <see cref="StringBuilder"/> with the text.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The tokens hold a <see cref="ReuseForEachToken"/> made by hand that is not linked to a FOREACH.
        /// Tokens from <see cref="Parser.Parse(string)"/> are always linked.</exception>
        /// <remarks>
        /// <para>Exceptions thrown by your providers are not caught; they reach the caller.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// StringBuilder text = processor.Process(new ProcessorArgs(tokens) { Context = order });
        /// File.WriteAllText("order.txt", text.ToString());
        /// </code>
        /// </example>
        public StringBuilder Process(ProcessorArgs args)
        {
            ArgumentNullException.ThrowIfNull(args);

            var retVal = new StringBuilder();
            var parentContext = new List<object?>();
            if (args.Context != null) { parentContext.Add(args.Context); }

            var vars = default(Variables);
            this.Process(retVal, args.TokenList, args.Context, parentContext, ref vars);

            return retVal;
        }

        /// <summary>
        /// Gets or sets the provider for <c>&lt;%IF name%&gt;</c> and <c>&lt;%ELSEIF name%&gt;</c>. It returns whether the named condition is true.
        /// For <c>&lt;%IF not name%&gt;</c> it gets the name without <c>not</c>, and the processor applies the <c>not</c>. The default is null.
        /// </summary>
        /// <example>
        /// <code>
        /// processor.ConditionValueProvider = args =&gt; args.Name switch
        /// {
        ///     "paid" =&gt; ((Invoice)args.Context!).Paid,
        ///     _ =&gt; false,
        /// };
        /// </code>
        /// </example>
        public Func<ConditionArgs, bool>? ConditionValueProvider { get; set; }

        /// <summary>
        /// Gets or sets the provider for <c>&lt;%FOREACH name%&gt;</c> and <c>&lt;%REUSE_FOREACH existing name%&gt;</c>. It returns the rows; each row is the
        /// context of one row of the loop. Null or an empty list writes the NORECORD part. The default is null.
        /// </summary>
        /// <example>
        /// <code>
        /// processor.LoopValueProvider = args =&gt; args.Name == "lines" ? ((Order)args.Context!).Lines : null;
        /// </code>
        /// </example>
        public Func<LoopArgs, IList?>? LoopValueProvider { get; set; }

        /// <summary>
        /// Gets or sets the provider for <c>&lt;%=name%&gt;</c>. It returns the text to write; null or an empty string writes nothing.
        /// It is not called when a SET variable with that name exists. The default is null.
        /// </summary>
        /// <example>
        /// <code>
        /// processor.TokenValueProvider = args =&gt; args.Name == "today" ? DateTime.Today.ToString(args.GetAttribute("format", "d")) : null;
        /// </code>
        /// </example>
        public Func<TokenArgs, string?>? TokenValueProvider { get; set; }

        /// <summary>
        /// Gets or sets the provider for <c>&lt;%WITH name%&gt;</c>. It returns the object that is the context inside the block; null skips the block.
        /// The default is null.
        /// </summary>
        /// <example>
        /// <code>
        /// processor.WithValueProvider = args =&gt; args.Name == "customer" ? ((Order)args.Context!).Customer : null;
        /// </code>
        /// </example>
        public Func<TokenArgs, object?>? WithValueProvider { get; set; }

        /// <summary>
        /// Gets or sets the provider for <c>&lt;%PROCESS_TEMPLATE name%&gt;</c>. It returns the parsed tokens of the sub template, which are processed
        /// in place with the current context. Null writes nothing. The default is null.
        /// </summary>
        /// <example>
        /// <code>
        /// var parts = new Dictionary&lt;string, List&lt;IToken&gt;&gt; { ["footer"] = parser.Parse("-- Sent by Toshal") };
        /// processor.ProcessTemplateValueProvider = args =&gt; parts.GetValueOrDefault(args.Name);
        /// </code>
        /// </example>
        public Func<ProcessTemplateArgs, List<IToken>?>? ProcessTemplateValueProvider { get; set; }

        // Runs the tokens of one block. vars is the scope of SET variables: a block that shares the scope of its parent (IF, ELSE, a SET value,
        // the rows of one FOREACH) gets the same ref; a block with its own scope gets Variables.Child, which copies only when a SET writes.
        private void Process(StringBuilder output, List<IToken> tokenList, object? context, List<object?> parentContext, ref Variables vars)
        {
            for (var i = 0; i < tokenList.Count; i++)
            {
                // Same order of checks as before: a class that derives from two token types is handled as the first one.
                switch (tokenList[i])
                {
                    case ContentToken contentToken:
                        output.Append(contentToken.Content);
                        break;

                    case NamedToken namedToken:
                        this.ProcessNamed(output, namedToken, context, parentContext, ref vars);
                        break;

                    case ConditionToken conditionToken:
                        if (this.ConditionValueProvider != null)
                        {
                            this.ProcessCondition(output, conditionToken, context, parentContext, ref vars);
                        }

                        break;

                    case ForEachToken forEachToken:
                        var loopValueProvider = this.LoopValueProvider;
                        if (loopValueProvider != null)
                        {
                            var args = new LoopArgs(forEachToken, context, parentContext);
                            this.ProcessForEach(output, context, args, forEachToken, loopValueProvider, ref vars);
                        }

                        break;

                    case ReuseForEachToken reuseForEachToken:
                        this.ProcessReuseForEach(output, reuseForEachToken, context, parentContext, ref vars);
                        break;

                    case WithToken withToken:
                        this.ProcessWith(output, withToken, context, parentContext, ref vars);
                        break;

                    case RemovePreviousNewLineToken:
                        RemovePreviousNewLine(output);
                        break;

                    case RemovePreviousCharsToken removeToken:
                        var count = removeToken.CharCount;
                        if (output.Length < count) count = output.Length;
                        output.Remove(output.Length - count, count);
                        break;

                    case ProcessTemplateToken processTemplateToken:
                        this.ProcessTemplate(output, processTemplateToken, context, parentContext, ref vars);
                        break;

                    case ContextAsStringToken:
                        if (context != null) output.Append(context.ToString());
                        break;

                    case SetToken setToken:
                        var valueOutput = new StringBuilder();
                        this.Process(valueOutput, setToken.InnerTokens, context, parentContext, ref vars);
                        vars.Set(setToken.Name, valueOutput.ToString());
                        break;
                }
            }
        }

        private void ProcessNamed(StringBuilder output, NamedToken namedToken, object? context, List<object?> parentContext, ref Variables vars)
        {
            if (vars.TryGet(namedToken.Name, out var value))
            {
                output.Append(value);
                return;
            }

            var tokenValueProvider = this.TokenValueProvider;
            if (tokenValueProvider == null) return; //skip as there is no value provider

            var val = tokenValueProvider(new TokenArgs(namedToken, context, parentContext));
            if (!string.IsNullOrEmpty(val))
            {
                output.Append(val);
            }
        }

        private void ProcessCondition(StringBuilder output, ConditionToken conditionToken, object? context, List<object?> parentContext, ref Variables vars)
        {
            var val = this.ConditionValueProvider!(new ConditionArgs(conditionToken, context, parentContext));

            if (conditionToken.IsPositive == val)
            {
                this.Process(output, conditionToken.InnerTokens, context, parentContext, ref vars);
            }
            else if (conditionToken.FalsePart is ElseToken elseToken)
            {
                this.Process(output, elseToken.InnerTokens, context, parentContext, ref vars);
            }
            else if (conditionToken.FalsePart is ConditionToken elseIfToken)
            {
                this.ProcessCondition(output, elseIfToken, context, parentContext, ref vars);
            }
        }

        private void ProcessReuseForEach(StringBuilder output, ReuseForEachToken reuseForEachToken, object? context, List<object?> parentContext, ref Variables vars)
        {
            var loopValueProvider = this.LoopValueProvider;
            if (loopValueProvider == null) return;

            // Parser.Parse links every REUSE_FOREACH. Only a token made by hand can be unlinked.
            var existing = reuseForEachToken.ExistingForEachToken
                ?? throw new InvalidOperationException("REUSE_FOREACH " + reuseForEachToken.Name + " is not linked to a FOREACH. Create tokens with Parser.Parse.");

            var args = new LoopArgs(reuseForEachToken.Name, context, parentContext, existing.Attributes);
            this.ProcessForEach(output, context, args, existing, loopValueProvider, ref vars);
        }

        private void ProcessWith(StringBuilder output, WithToken withToken, object? context, List<object?> parentContext, ref Variables vars)
        {
            var withValueProvider = this.WithValueProvider;
            if (withValueProvider == null) return; //skip as there is no value provider

            var val = withValueProvider(new TokenArgs(withToken, context, parentContext));
            if (val != null)
            {
                parentContext.Add(val);
                var withVars = Variables.Child(vars);
                this.Process(output, withToken.InnerTokens, val, parentContext, ref withVars);
                RemoveLast(parentContext, val);
            }
        }

        private void ProcessTemplate(StringBuilder output, ProcessTemplateToken processTemplateToken, object? context, List<object?> parentContext, ref Variables vars)
        {
            var processTemplateValueProvider = this.ProcessTemplateValueProvider;
            if (processTemplateValueProvider == null) return; //skip as there is no value provider

            var val = processTemplateValueProvider(new ProcessTemplateArgs(processTemplateToken, context, parentContext));
            if (val != null)
            {
                var templateVars = Variables.Child(vars);
                this.Process(output, val, context, parentContext, ref templateVars);
            }
        }

        private static void RemovePreviousNewLine(StringBuilder output)
        {
            if (output.Length == 0) return;

            if (output[output.Length - 1] == '\n')
                output.Remove(output.Length - 1, 1);

            if (output.Length > 0 && output[output.Length - 1] == '\r')
                output.Remove(output.Length - 1, 1);
        }

        private void ProcessForEach(StringBuilder output, object? context, LoopArgs args, ForEachToken forEachToken, Func<LoopArgs, IList?> loopValueProvider, ref Variables vars)
        {
            var val = loopValueProvider(args);
            if (val == null || val.Count == 0)
            {
                if (forEachToken.NoRecordTokens.Count > 0)
                {
                    var noRecordVars = Variables.Child(vars);
                    this.Process(output, forEachToken.NoRecordTokens, context, args.ParentContext, ref noRecordVars);
                }

                return;
            }

            // The rows share one scope: a SET in a row is seen by the next rows, but not after the loop.
            var rowVars = Variables.Child(vars);

            args.ParentContext.Add(val);

            if (forEachToken.HeaderTokens.Count > 0)
            {
                var headerVars = Variables.Child(vars);
                this.Process(output, forEachToken.HeaderTokens, val, args.ParentContext, ref headerVars);
            }

            for (var i = 0; i < val.Count; i++)
            {
                var item = val[i];
                var isAlt = i % 2 == 1;

                args.ParentContext.Add(item);

                var beforeTokens = (isAlt && forEachToken.BeforeAltRowTokens.Count > 0)
                                       ? forEachToken.BeforeAltRowTokens
                                       : forEachToken.BeforeRowTokens;

                var afterTokens = (isAlt && forEachToken.AfterAltRowTokens.Count > 0)
                                      ? forEachToken.AfterAltRowTokens
                                      : forEachToken.AfterRowTokens;

                var rowTokens = (isAlt && forEachToken.AltRowTokens.Count > 0)
                                    ? forEachToken.AltRowTokens
                                    : forEachToken.RowTokens;

                // Which parts a row uses. By design the LAST parts win over the FIRST parts:
                // - the first row uses the FIRST parts only when the list has more than one row,
                // - the last row uses the LAST parts, so a list with one row uses the LAST parts and never the FIRST parts.
                // An empty part is ignored and the choice above stays (ALT parts on odd rows, else the normal parts).
                if (i == 0 && val.Count > 1)
                {
                    beforeTokens = (forEachToken.BeforeFirstRowTokens.Count > 0)
                                       ? forEachToken.BeforeFirstRowTokens
                                       : beforeTokens;

                    afterTokens = (forEachToken.AfterFirstRowTokens.Count > 0)
                                      ? forEachToken.AfterFirstRowTokens
                                      : afterTokens;

                    rowTokens = (forEachToken.FirstRowTokens.Count > 0) ? forEachToken.FirstRowTokens : rowTokens;
                }
                else if (i == val.Count - 1)
                {
                    beforeTokens = (forEachToken.BeforeLastRowTokens.Count > 0)
                                       ? forEachToken.BeforeLastRowTokens
                                       : beforeTokens;

                    afterTokens = (forEachToken.AfterLastRowTokens.Count > 0)
                                      ? forEachToken.AfterLastRowTokens
                                      : afterTokens;

                    rowTokens = (forEachToken.LastRowTokens.Count > 0) ? forEachToken.LastRowTokens : rowTokens;
                }

                if (beforeTokens.Count > 0)
                {
                    var beforeVars = Variables.Child(rowVars);
                    this.Process(output, beforeTokens, item, args.ParentContext, ref beforeVars);
                }

                if (rowTokens.Count > 0)
                {
                    this.Process(output, rowTokens, item, args.ParentContext, ref rowVars);
                }

                if (afterTokens.Count > 0)
                {
                    var afterVars = Variables.Child(rowVars);
                    this.Process(output, afterTokens, item, args.ParentContext, ref afterVars);
                }

                RemoveLast(args.ParentContext, item);
            }

            if (forEachToken.FooterTokens.Count > 0)
            {
                var footerVars = Variables.Child(vars);
                this.Process(output, forEachToken.FooterTokens, val, args.ParentContext, ref footerVars);
            }

            RemoveLast(args.ParentContext, val);
        }

        // The parent context is a stack, so the entry a block added is the last one equal to it.
        private static void RemoveLast(List<object?> parentContext, object? value)
        {
            int index = parentContext.LastIndexOf(value);
            if (index >= 0)
            {
                parentContext.RemoveAt(index);
            }
        }

        // The SET variables of one scope. A child scope shares the dictionary of its parent and copies it only when a SET writes,
        // so blocks without SET allocate nothing. The parent never runs while a child scope is in use, so the shared dictionary cannot change under it.
        private struct Variables
        {
            private Dictionary<string, string>? map;
            private bool owned;

            public static Variables Child(Variables parent) => new Variables { map = parent.map };

            public readonly bool TryGet(string name, [NotNullWhen(true)] out string? value)
            {
                if (this.map == null)
                {
                    value = null;
                    return false;
                }

                return this.map.TryGetValue(name, out value);
            }

            public void Set(string name, string value)
            {
                if (!this.owned)
                {
                    this.map = this.map == null ? new Dictionary<string, string>() : new Dictionary<string, string>(this.map);
                    this.owned = true;
                }

                this.map[name] = value;
            }
        }
    }
}
