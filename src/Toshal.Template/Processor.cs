// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;

    using Toshal.Template.Processing;
    using Toshal.Template.Tokens;

    /// <summary>
    /// Turns parsed tokens into text. The data comes from five provider delegates that you set: one for each kind of tag that needs data.
    /// The processor does not read properties by reflection; your providers decide what each name means.
    /// </summary>
    /// <remarks>
    /// <para><b>Warning:</b> when a provider is not set, its tags are skipped without an error. An IF block (with its ELSE) is skipped when
    /// <see cref="ConditionValueProvider"/> is null, a FOREACH block (with its NORECORD) when <see cref="LoopValueProvider"/> is null, and so on.
    /// A <c>&lt;%=name%&gt;</c> still writes a SET variable of that name when there is one.</para>
    /// <para>The processor keeps no state between calls other than the provider properties, so one instance can process many templates,
    /// also from many threads at once.</para>
    /// <para><b>Warning:</b> to save allocations, one call of <see cref="Process(ProcessorArgs)"/> reuses one <see cref="TokenArgs"/> for all value and
    /// WITH tags and one <see cref="ConditionArgs"/> for all IF and ELSEIF tags. An args object is valid only while your provider runs; do not keep it.</para>
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
            this.Process(args, retVal);
            return retVal;
        }

        /// <summary>
        /// Processes the tokens with the given top context and appends the text to a builder you give. Reuse one builder (call
        /// <see cref="StringBuilder.Clear"/> between runs) to avoid a new builder, its growth and the copy of <c>ToString()</c> for every run.
        /// </summary>
        /// <param name="args">The tokens and the top context.</param>
        /// <param name="output">The builder to append to. Text already in it stays.</param>
        /// <exception cref="ArgumentNullException"><paramref name="args"/> or <paramref name="output"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The tokens hold a <see cref="ReuseForEachToken"/> made by hand that is not linked to a FOREACH.
        /// Tokens from <see cref="Parser.Parse(string)"/> are always linked.</exception>
        /// <remarks>
        /// <para><c>&lt;%REMOVE_PREVIOUS n%&gt;</c> and <c>&lt;%REMOVE_PREVIOUS_NEW_LINE%&gt;</c> remove only text that this call wrote, never text
        /// that was in <paramref name="output"/> before the call.</para>
        /// <para>Exceptions thrown by your providers are not caught; they reach the caller. The text written before the exception stays in
        /// <paramref name="output"/>.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var output = new StringBuilder();
        /// foreach (var order in orders)
        /// {
        ///     output.Clear();
        ///     processor.Process(new ProcessorArgs(tokens) { Context = order }, output);
        ///     writer.Write(output);   // TextWriter.Write(StringBuilder) copies no string
        /// }
        /// </code>
        /// </example>
        public void Process(ProcessorArgs args, StringBuilder output)
        {
            ArgumentNullException.ThrowIfNull(args);
            ArgumentNullException.ThrowIfNull(output);

            var parentContext = new List<object?>();
            if (args.Context != null) { parentContext.Add(args.Context); }

            var vars = default(TemplateVariables);
            this.Process(output, args.TokenList, args.Context, new ProcessRun(parentContext, output), ref vars);
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
        /// Gets or sets a writer for <c>&lt;%=name%&gt;</c> that appends the value straight to the output, so no string is made for numbers,
        /// dates or text joined from parts. When it is set, it is used instead of <see cref="TokenValueProvider"/>. The default is null.
        /// </summary>
        /// <remarks>
        /// <para>A SET variable with the name still wins: the writer is not called for it. Inside a SET block the writer appends to the value of the SET.</para>
        /// <para><b>Warning:</b> only append. Do not remove or change text that is already in the builder; it belongs to the template.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// processor.TokenWriter = (args, output) =&gt;
        /// {
        ///     if (args.Name == "count") output.Append(((Order)args.Context!).Lines.Count);
        /// };
        /// </code>
        /// </example>
        public Action<TokenArgs, StringBuilder>? TokenWriter { get; set; }

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
        // the rows of one FOREACH) gets the same ref; a block with its own scope gets TemplateVariables.Child, which copies only when a SET writes.
        private void Process(StringBuilder output, List<IToken> tokenList, object? context, ProcessRun run, ref TemplateVariables vars)
        {
            for (var i = 0; i < tokenList.Count; i++)
            {
                // The token classes are sealed, so each case is one type compare. The most common tokens come first.
                switch (tokenList[i])
                {
                    case ContentToken contentToken:
                        output.Append(contentToken.Content);
                        break;

                    case NamedToken namedToken:
                        this.ProcessNamed(output, namedToken, context, run, ref vars);
                        break;

                    case ConditionToken conditionToken:
                        if (this.ConditionValueProvider != null)
                        {
                            this.ProcessCondition(output, conditionToken, context, run, ref vars);
                        }

                        break;

                    case ForEachToken forEachToken:
                        var loopValueProvider = this.LoopValueProvider;
                        if (loopValueProvider != null)
                        {
                            var args = new LoopArgs(forEachToken, context, run.ParentContext);
                            this.ProcessForEach(output, context, args, forEachToken, loopValueProvider, run, ref vars);
                        }

                        break;

                    case ReuseForEachToken reuseForEachToken:
                        this.ProcessReuseForEach(output, reuseForEachToken, context, run, ref vars);
                        break;

                    case WithToken withToken:
                        this.ProcessWith(output, withToken, context, run, ref vars);
                        break;

                    case RemovePreviousNewLineToken:
                        run.RemovePreviousNewLine(output);
                        break;

                    case RemovePreviousCharsToken removeToken:
                        run.RemovePrevious(output, removeToken.CharCount);
                        break;

                    case ProcessTemplateToken processTemplateToken:
                        this.ProcessTemplate(output, processTemplateToken, context, run, ref vars);
                        break;

                    case ContextAsStringToken:
                        if (context != null) output.Append(context.ToString());
                        break;

                    case SeparatorToken separatorToken:
                        if (run.InRowBeforeTheLast)
                        {
                            this.Process(output, separatorToken.InnerTokens, context, run, ref vars);
                        }

                        break;

                    case SetToken setToken:
                        var valueOutput = run.RentBuilder();
                        this.Process(valueOutput, setToken.InnerTokens, context, run, ref vars);
                        vars.Set(setToken.Name, valueOutput.ToString());
                        run.ReturnBuilder(valueOutput);
                        break;
                }
            }
        }

        private void ProcessNamed(StringBuilder output, NamedToken namedToken, object? context, ProcessRun run, ref TemplateVariables vars)
        {
            if (vars.TryGet(namedToken.Name, out var value))
            {
                output.Append(value);
                return;
            }

            var tokenWriter = this.TokenWriter;
            if (tokenWriter != null)
            {
                tokenWriter(run.TokenArgs(namedToken, context), output);
                return;
            }

            var tokenValueProvider = this.TokenValueProvider;
            if (tokenValueProvider == null) return; //skip as there is no value provider

            var val = tokenValueProvider(run.TokenArgs(namedToken, context));
            if (!string.IsNullOrEmpty(val))
            {
                output.Append(val);
            }
        }

        private void ProcessCondition(StringBuilder output, ConditionToken conditionToken, object? context, ProcessRun run, ref TemplateVariables vars)
        {
            var val = this.ConditionValueProvider!(run.ConditionArgs(conditionToken, context));

            if (conditionToken.IsPositive == val)
            {
                this.Process(output, conditionToken.InnerTokens, context, run, ref vars);
            }
            else if (conditionToken.FalsePart is ElseToken elseToken)
            {
                this.Process(output, elseToken.InnerTokens, context, run, ref vars);
            }
            else if (conditionToken.FalsePart is ConditionToken elseIfToken)
            {
                this.ProcessCondition(output, elseIfToken, context, run, ref vars);
            }
        }

        private void ProcessReuseForEach(StringBuilder output, ReuseForEachToken reuseForEachToken, object? context, ProcessRun run, ref TemplateVariables vars)
        {
            var loopValueProvider = this.LoopValueProvider;
            if (loopValueProvider == null) return;

            // Parser.Parse links every REUSE_FOREACH. Only a token made by hand can be unlinked.
            var existing = reuseForEachToken.ExistingForEachToken
                ?? throw new InvalidOperationException("REUSE_FOREACH " + reuseForEachToken.Name + " is not linked to a FOREACH. Create tokens with Parser.Parse.");

            var args = new LoopArgs(reuseForEachToken.Name, context, run.ParentContext, existing.Attributes);
            this.ProcessForEach(output, context, args, existing, loopValueProvider, run, ref vars);
        }

        private void ProcessWith(StringBuilder output, WithToken withToken, object? context, ProcessRun run, ref TemplateVariables vars)
        {
            var withValueProvider = this.WithValueProvider;
            if (withValueProvider == null) return; //skip as there is no value provider

            var val = withValueProvider(run.TokenArgs(withToken, context));
            if (val != null)
            {
                run.ParentContext.Add(val);
                var withVars = TemplateVariables.Child(vars);
                this.Process(output, withToken.InnerTokens, val, run, ref withVars);
                run.RemoveParent(val);
            }
        }

        private void ProcessTemplate(StringBuilder output, ProcessTemplateToken processTemplateToken, object? context, ProcessRun run, ref TemplateVariables vars)
        {
            var processTemplateValueProvider = this.ProcessTemplateValueProvider;
            if (processTemplateValueProvider == null) return; //skip as there is no value provider

            var val = processTemplateValueProvider(new ProcessTemplateArgs(processTemplateToken, context, run.ParentContext));
            if (val != null)
            {
                var templateVars = TemplateVariables.Child(vars);
                var indent = processTemplateToken.Indent;
                if (string.IsNullOrEmpty(indent))
                {
                    this.Process(output, val, context, run, ref templateVars);
                    return;
                }

                // Write the sub template into its own builder, then copy it with the indent after each line break.
                var subOutput = run.RentBuilder();
                this.Process(subOutput, val, context, run, ref templateVars);
                ProcessRun.AppendIndented(output, subOutput, indent);
                run.ReturnBuilder(subOutput);
            }
        }

        private void ProcessForEach(StringBuilder output, object? context, LoopArgs args, ForEachToken forEachToken, Func<LoopArgs, IList?> loopValueProvider, ProcessRun run, ref TemplateVariables vars)
        {
            var val = loopValueProvider(args);
            if (val == null || val.Count == 0)
            {
                if (forEachToken.NoRecordTokens.Count > 0)
                {
                    var noRecordVars = TemplateVariables.Child(vars);
                    run.EnterLoop();
                    this.Process(output, forEachToken.NoRecordTokens, context, run, ref noRecordVars);
                    run.ExitLoop();
                }

                return;
            }

            run.EnterLoop();

            // The rows share one scope: a SET in a row is seen by the next rows, but not after the loop.
            var rowVars = TemplateVariables.Child(vars);

            run.ParentContext.Add(val);

            if (forEachToken.HeaderTokens.Count > 0)
            {
                var headerVars = TemplateVariables.Child(vars);
                this.Process(output, forEachToken.HeaderTokens, val, run, ref headerVars);
            }

            for (var i = 0; i < val.Count; i++)
            {
                var item = val[i];
                var isAlt = i % 2 == 1;

                run.ParentContext.Add(item);
                run.SetRow(isLast: i == val.Count - 1);

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
                    var beforeVars = TemplateVariables.Child(rowVars);
                    this.Process(output, beforeTokens, item, run, ref beforeVars);
                }

                if (rowTokens.Count > 0)
                {
                    this.Process(output, rowTokens, item, run, ref rowVars);
                }

                if (afterTokens.Count > 0)
                {
                    var afterVars = TemplateVariables.Child(rowVars);
                    this.Process(output, afterTokens, item, run, ref afterVars);
                }

                run.RemoveParent(item);
            }

            run.LeaveRow();

            if (forEachToken.FooterTokens.Count > 0)
            {
                var footerVars = TemplateVariables.Child(vars);
                this.Process(output, forEachToken.FooterTokens, val, run, ref footerVars);
            }

            run.RemoveParent(val);
            run.ExitLoop();
        }
    }
}
