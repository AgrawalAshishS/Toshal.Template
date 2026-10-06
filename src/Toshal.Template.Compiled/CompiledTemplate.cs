// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Compiled
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using Toshal.Template.Processing;
    using Toshal.Template.Tokens;

    /// <summary>
    /// The base class of a compiled template: a C# class made at build time from a <c>.ctt</c> file by Toshal.Template.Generator or by the
    /// <c>toshal-template</c> tool. The template text is already turned into code, so no parser runs and no token list is walked at run time.
    /// </summary>
    /// <remarks>
    /// <para>The generated class gets its data from partial methods that you write in the other half of the class (a stub is made once for you):
    /// <c>TokenValue</c> (or <c>WriteToken</c>) for <c>&lt;%=name%&gt;</c>, <c>Condition</c> for IF and ELSEIF, <c>Loop</c> for FOREACH and REUSE_FOREACH,
    /// <c>With</c> for WITH and <c>SubTemplate</c> for PROCESS_TEMPLATE. Only the methods for the tags the template uses are declared.</para>
    /// <para>The text is the same as <see cref="Processor"/> writes for the same template and the same provider answers.</para>
    /// <para>A call of <c>Process</c> keeps its state in its own objects, so one instance can be used from many threads at once, as long as your
    /// partial methods are safe for that.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Templates/Hello.ctt:   Hello &lt;%=Name%&gt;!
    /// // Hello.cs (the stub, filled in by you):
    /// public partial class Hello
    /// {
    ///     private partial string? TokenValue(TokenArgs args) =&gt; args.Name switch
    ///     {
    ///         "name" =&gt; ((Customer)args.Context!).Name,
    ///         _ =&gt; null,
    ///     };
    /// }
    ///
    /// string text = new Hello().Process(customer).ToString();   // "Hello Asha!"
    /// </code>
    /// </example>
    public abstract class CompiledTemplate
    {
        /// <summary>
        /// Writes the template with the given top context and returns the text.
        /// </summary>
        /// <param name="context">The top context. Your partial methods get it as <see cref="ArgsBase.Context"/> for tags outside any FOREACH or WITH.
        /// When it is not null it is also the first entry of <see cref="ArgsBase.ParentContext"/>.</param>
        /// <returns>A new <see cref="StringBuilder"/> with the text.</returns>
        /// <remarks>
        /// <para>Exceptions thrown by your partial methods are not caught; they reach the caller.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// string text = new OrderConfirm().Process(order).ToString();
        /// </code>
        /// </example>
        public StringBuilder Process(object? context = null)
        {
            var retVal = new StringBuilder();
            this.Process(context, retVal);
            return retVal;
        }

        /// <summary>
        /// Writes the template with the given top context and appends the text to a builder you give. Reuse one builder (call
        /// <see cref="StringBuilder.Clear"/> between runs) to avoid a new builder, its growth and the copy of <c>ToString()</c> for every run.
        /// </summary>
        /// <param name="context">The top context, as for <see cref="Process(object?)"/>.</param>
        /// <param name="output">The builder to append to. Text already in it stays.</param>
        /// <exception cref="ArgumentNullException"><paramref name="output"/> is null.</exception>
        /// <remarks>
        /// <para><c>&lt;%REMOVE_PREVIOUS n%&gt;</c> and <c>&lt;%REMOVE_PREVIOUS_NEW_LINE%&gt;</c> remove only text that this call wrote, never text
        /// that was in <paramref name="output"/> before the call.</para>
        /// <para>Exceptions thrown by your partial methods are not caught; they reach the caller. The text written before the exception stays in
        /// <paramref name="output"/>.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var template = new OrderConfirm();
        /// var output = new StringBuilder();
        /// foreach (var order in orders)
        /// {
        ///     output.Clear();
        ///     template.Process(order, output);
        ///     writer.Write(output);
        /// }
        /// </code>
        /// </example>
        public void Process(object? context, StringBuilder output)
        {
            ArgumentNullException.ThrowIfNull(output);

            var parentContext = new List<object?>();
            if (context != null) { parentContext.Add(context); }

            var scope = default(TemplateScope);
            this.Render(output, context, new TemplateRun(parentContext, output), ref scope);
        }

        /// <summary>
        /// Writes the template. The generated half of the class implements it; you do not.
        /// </summary>
        /// <param name="output">The builder to append to: the main output, or the value of a SET block.</param>
        /// <param name="context">The current context.</param>
        /// <param name="run">The state of this call.</param>
        /// <param name="scope">The SET variables of the current block.</param>
        /// <example>
        /// <code>
        /// protected override void Render(StringBuilder output, object? context, TemplateRun run, ref TemplateScope scope)
        /// {
        ///     output.Append("Hello");
        /// }
        /// </code>
        /// </example>
        protected abstract void Render(StringBuilder output, object? context, TemplateRun run, ref TemplateScope scope);

        /// <summary>
        /// Makes the attributes of one tag, with their lower case copy. Generated code calls it once per tag, in a static field.
        /// </summary>
        /// <param name="namesAndValues">Pairs of lower case attribute name and value as written.</param>
        /// <returns>The attributes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="namesAndValues"/> is null.</exception>
        /// <exception cref="ArgumentException">The array has an odd length, or a name is repeated.</exception>
        /// <example>
        /// <code>
        /// private static readonly TokenAttributeDictionary A0 = Attributes("format", "0.00");
        /// </code>
        /// </example>
        protected static TokenAttributeDictionary Attributes(params string[] namesAndValues)
        {
            ArgumentNullException.ThrowIfNull(namesAndValues);
            if (namesAndValues.Length % 2 != 0)
            {
                throw new ArgumentException("Give names and values in pairs.", nameof(namesAndValues));
            }

            var retVal = new TokenAttributeDictionary();
            for (int i = 0; i < namesAndValues.Length; i += 2)
            {
                if (!retVal.TryAddAttribute(namesAndValues[i], namesAndValues[i + 1]))
                {
                    throw new ArgumentException("The attribute " + namesAndValues[i] + " is repeated.", nameof(namesAndValues));
                }
            }

            return retVal;
        }

        /// <summary>
        /// Does <c>&lt;%PROCESS_TEMPLATE name%&gt;</c>: writes another compiled template in place, with the current context. Its SET variables stay
        /// inside it. When <paramref name="indent"/> is not empty, every line of it after the first gets the indent.
        /// </summary>
        /// <param name="template">The sub template your <c>SubTemplate</c> method returned. Null writes nothing.</param>
        /// <param name="output">The builder being written.</param>
        /// <param name="context">The current context.</param>
        /// <param name="run">The state of this call.</param>
        /// <param name="scope">The SET variables of the current block; the sub template sees them.</param>
        /// <param name="indent">The spaces or tabs in front of the tag when it is alone on its line, else empty.</param>
        /// <example>
        /// <code>
        /// ProcessSubTemplate(this.SubTemplate(run.ProcessTemplateArgs("footer", A0, context)), output, context, run, ref scope, "    ");
        /// </code>
        /// </example>
        protected static void ProcessSubTemplate(CompiledTemplate? template, StringBuilder output, object? context, TemplateRun run, ref TemplateScope scope, string indent)
        {
            if (template == null) return;

            var templateScope = TemplateScope.Child(scope);
            if (string.IsNullOrEmpty(indent))
            {
                template.Render(output, context, run, ref templateScope);
                return;
            }

            // Write the sub template into its own builder, then copy it with the indent after each line break.
            var subOutput = run.RentBuilder();
            template.Render(subOutput, context, run, ref templateScope);
            ProcessRun.AppendIndented(output, subOutput, indent);
            run.ReturnBuilder(subOutput);
        }
    }
}
