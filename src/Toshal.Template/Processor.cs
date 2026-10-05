// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
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
        /// <param name="args">The tokens and the top context. Must not be null.</param>
        /// <returns>A new <see cref="StringBuilder"/> with the text.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Known issue: the template has a <c>&lt;%REMOVE_PREVIOUS n%&gt;</c> with a negative n. See docs/known-issues.md.</exception>
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
            var retVal = new StringBuilder();
            var parentContext = new List<object?>();
            if (args.Context != null) { parentContext.Add(args.Context); }

            this.Process(retVal, args.TokenList, args.Context, parentContext, new Dictionary<string, string>());

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

        private bool HandleConditionToken(StringBuilder output, IToken token, object? context, List<object?> parentContext, Dictionary<string, string> customTokens)
        {
            var conditionToken = token as ConditionToken;
            if (conditionToken == null) return false;
            if (this.ConditionValueProvider == null) return true; //skip as there is no value provider

            var args = new ConditionArgs(conditionToken, context, parentContext);
            var val = this.ConditionValueProvider(args);

            if (conditionToken.IsPositive == val)
            {
                this.Process(output, conditionToken.InnerTokens, context, parentContext, customTokens);
            }
            else if (conditionToken.FalsePart != null)
            {
                var elseToken = conditionToken.FalsePart as ElseToken;
                if (elseToken != null)
                {
                    this.Process(output, elseToken.InnerTokens, context, parentContext, customTokens);
                }
                else
                {
                    this.HandleConditionToken(output, conditionToken.FalsePart, context, parentContext, customTokens);
                }
            }

            return true;
        }

        private bool HandleForEachToken(StringBuilder output, IToken token, object? context, List<object?> parentContext, Dictionary<string, string> customTokens)
        {
            var forEachToken = token as ForEachToken;
            if (forEachToken == null) return false;
            if (this.LoopValueProvider == null) return true; //skip as there is no value provider

            var args = new LoopArgs(forEachToken, context, parentContext);
            return this.ProcessForEach(output, context, args, forEachToken, customTokens);
        }

        private bool HandleNamedToken(StringBuilder output, IToken token, object? context, List<object?> parentContext, Dictionary<string, string> customTokens)
        {
            var namedToken = token as NamedToken;
            if (namedToken == null) return false;

            string? value = "";
            if (customTokens.TryGetValue(namedToken.Name, out value))
            {
                output.Append(value);
                return true;
            }

            if (this.TokenValueProvider == null) return true; //skip as there is no value provider

            var args = new TokenArgs(namedToken, context, parentContext);
            var val = this.TokenValueProvider(args);

            if (!string.IsNullOrEmpty(val))
            {
                output.Append(val);
            }

            return true;
        }

        private bool HandleReuseForEachToken(StringBuilder output, IToken token, object? context, List<object?> parentContext, Dictionary<string, string> customTokens)
        {
            var reuseForEachToken = token as ReuseForEachToken;
            if (reuseForEachToken == null) return false;
            if (this.LoopValueProvider == null) return true;

            var args = new LoopArgs(reuseForEachToken.Name, context, parentContext, reuseForEachToken.ExistingForEachToken!.Attributes);
            return this.ProcessForEach(output, context, args, reuseForEachToken.ExistingForEachToken!, customTokens);
        }

        private bool HandleWithToken(StringBuilder output, IToken token, object? context, List<object?> parentContext, Dictionary<string, string> customTokens)
        {
            var withToken = token as WithToken;
            if (withToken == null) return false;
            if (this.WithValueProvider == null) return true;//skip as there is no value provider;

            var args = new TokenArgs(withToken, context, parentContext);

            var val = this.WithValueProvider(args);
            if (val != null)
            {
                parentContext.Add(val);
                var newCustomTokens = new Dictionary<string, string>(customTokens);
                this.Process(output, withToken.InnerTokens, val, parentContext, newCustomTokens);
                RemoveLast(parentContext, val);
            }

            return true;
        }

        private bool HandleSetToken(IToken token, object? context, List<object?> parentContext, ref Dictionary<string, string> customTokens)
        {
            var setToken = token as SetToken;
            if (setToken == null) return false;

            var valueOutput = new StringBuilder();
            this.Process(valueOutput, setToken.InnerTokens, context, parentContext, customTokens);

            if (customTokens.ContainsKey(setToken.Name) == false)
            {
                customTokens.Add(setToken.Name, valueOutput.ToString());
            }
            else
            {
                customTokens[setToken.Name] = valueOutput.ToString();
            }

            return true;
        }

        private bool HandleRemovePreviousNewLine(StringBuilder output, IToken token, object? context, Dictionary<string, string> customTokens)
        {
            var removeToken = token as RemovePreviousNewLineToken;
            if (removeToken == null) return false;

            if (output.Length == 0) return true;

            if (output[output.Length - 1] == '\n')
                output.Remove(output.Length - 1, 1);

            if (output.Length > 0 && output[output.Length - 1] == '\r')
                output.Remove(output.Length - 1, 1);

            return true;
        }

        private bool HandleRemovePreviousChars(StringBuilder output, IToken token, object? context, Dictionary<string, string> customTokens)
        {
            var removeToken = token as RemovePreviousCharsToken;
            if (removeToken == null) return false;

            var count = removeToken.CharCount;
            if (output.Length < removeToken.CharCount) count = output.Length;

            output.Remove(output.Length - count, count);

            return true;
        }

        private bool HandleProcessTemplateToken(StringBuilder output, IToken token, object? context, List<object?> parentContext, Dictionary<string, string> customTokens)
        {
            var processTemplateToken = token as ProcessTemplateToken;
            if (processTemplateToken == null) return false;
            if (this.ProcessTemplateValueProvider == null) return true;//skip as there is no value provider;

            var args = new ProcessTemplateArgs(processTemplateToken, context, parentContext);

            var val = this.ProcessTemplateValueProvider(args);
            if (val != null)
            {
                var newCustomTokens = new Dictionary<string, string>(customTokens);
                this.Process(output, val, context, parentContext, newCustomTokens);
            }

            return true;
        }

        private bool HandleContextAsString(StringBuilder output, IToken token, object? context)
        {
            var contextToken = token as ContextAsStringToken;
            if (contextToken == null) return false;

            if (context == null) return true;

            output.Append(context.ToString());

            return true;
        }

        private void Process(StringBuilder output, List<IToken> tokenList, object? context, List<object?> parentContext, Dictionary<string, string> customTokens)
        {
            foreach (var token in tokenList)
            {
                var contentToken = token as ContentToken;
                if (contentToken != null)
                {
                    output.Append(contentToken.Content);
                    continue;
                }

                if (this.HandleNamedToken(output, token, context, parentContext, customTokens))
                {
                    continue;
                }

                if (this.HandleConditionToken(output, token, context, parentContext, customTokens))
                {
                    continue;
                }

                if (this.HandleForEachToken(output, token, context, parentContext, customTokens))
                {
                    continue;
                }

                if (this.HandleReuseForEachToken(output, token, context, parentContext, customTokens))
                {
                    continue;
                }

                if (this.HandleWithToken(output, token, context, parentContext, customTokens))
                {
                    continue;
                }

                if (this.HandleRemovePreviousNewLine(output, token, context, customTokens))
                {
                    continue;
                }

                if (this.HandleRemovePreviousChars(output, token, context, customTokens))
                {
                    continue;
                }

                if (this.HandleProcessTemplateToken(output, token, context, parentContext, customTokens))
                {
                    continue;
                }

                if (this.HandleContextAsString(output, token, context))
                {
                    continue;
                }

                HandleSetToken(token, context, parentContext, ref customTokens);
            }
        }

        private bool ProcessForEach(StringBuilder output, object? context, LoopArgs args, ForEachToken forEachToken, Dictionary<string, string> customTokens)
        {
            var rowLevelShared = new Dictionary<string, string>(customTokens);

            var val = this.LoopValueProvider!(args);
            if (val == null || val.Count == 0)
            {
                if (forEachToken.NoRecordTokens.Count > 0)
                {
                    var newCustomTokens = new Dictionary<string, string>(customTokens);
                    this.Process(output, forEachToken.NoRecordTokens, context, args.ParentContext, newCustomTokens);
                }

                return true;
            }

            args.ParentContext.Add(val);

            if (forEachToken.HeaderTokens.Count > 0)
            {
                var newCustomTokens = new Dictionary<string, string>(customTokens);
                this.Process(output, forEachToken.HeaderTokens, val, args.ParentContext, newCustomTokens);
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
                    var newCustomTokens = new Dictionary<string, string>(rowLevelShared);
                    this.Process(output, beforeTokens, item, args.ParentContext, newCustomTokens);
                }

                if (rowTokens.Count > 0)
                {
                    this.Process(output, rowTokens, item, args.ParentContext, rowLevelShared);
                }

                if (afterTokens.Count > 0)
                {
                    var newCustomTokens = new Dictionary<string, string>(rowLevelShared);
                    this.Process(output, afterTokens, item, args.ParentContext, newCustomTokens);
                }

                RemoveLast(args.ParentContext, item);
            }

            if (forEachToken.FooterTokens.Count > 0)
            {
                var newCustomTokens = new Dictionary<string, string>(customTokens);
                this.Process(output, forEachToken.FooterTokens, val, args.ParentContext, newCustomTokens);
            }

            RemoveLast(args.ParentContext, val);

            return true;
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
    }
}