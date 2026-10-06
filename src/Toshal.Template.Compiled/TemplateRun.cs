// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Compiled
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Text;

    using Toshal.Template.Processing;
    using Toshal.Template.Tokens;

    /// <summary>
    /// The state of one call of <see cref="CompiledTemplate.Process(object?, StringBuilder)"/>: the parent context stack, the FOREACH rows being
    /// written and the args objects that every provider call reuses. Generated code uses it; you do not need it in your own code.
    /// </summary>
    /// <remarks>
    /// <para>It is the same state <see cref="Processor"/> keeps, so a compiled template writes the same text as the processor.</para>
    /// <para><b>Warning:</b> the args objects it returns are reused for the next tag. They are valid only while your provider runs; do not keep them.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Generated code for &lt;%=name%&gt;:
    /// var value = this.TokenValue(run.TokenArgs("name", A0, context));
    /// </code>
    /// </example>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class TemplateRun
    {
        private readonly ProcessRun run;

        internal TemplateRun(List<object?> parentContext, StringBuilder output)
        {
            this.run = new ProcessRun(parentContext, output);
        }

        /// <summary>
        /// Gets the stack of the outer contexts: the top context, then each FOREACH list, FOREACH row and WITH object around the current tag.
        /// </summary>
        /// <example>
        /// <code>
        /// run.ParentContext.Add(row);
        /// </code>
        /// </example>
        public List<object?> ParentContext => this.run.ParentContext;

        /// <summary>
        /// Gets whether a row of the innermost FOREACH is being written and it is not the last row. SEPARATOR blocks are written only then.
        /// </summary>
        /// <example>
        /// <code>
        /// if (run.InRowBeforeTheLast) output.Append(", ");
        /// </code>
        /// </example>
        public bool InRowBeforeTheLast => this.run.InRowBeforeTheLast;

        /// <summary>
        /// Gets the args for a value or WITH tag. The same object is reused for every such tag of this run.
        /// </summary>
        /// <param name="name">The lower case name of the tag.</param>
        /// <param name="attributes">The attributes of the tag.</param>
        /// <param name="context">The current context.</param>
        /// <returns>The args, pointed at this tag.</returns>
        /// <example>
        /// <code>
        /// TokenArgs args = run.TokenArgs("name", A0, context);
        /// </code>
        /// </example>
        public TokenArgs TokenArgs(string name, TokenAttributeDictionary attributes, object? context) => this.run.TokenArgs(name, attributes, context);

        /// <summary>
        /// Gets the args for an IF or ELSEIF tag. The same object is reused for every such tag of this run.
        /// </summary>
        /// <param name="name">The lower case name of the condition, without <c>not</c>.</param>
        /// <param name="attributes">The attributes of the tag.</param>
        /// <param name="context">The current context.</param>
        /// <returns>The args, pointed at this tag.</returns>
        /// <example>
        /// <code>
        /// if (this.Condition(run.ConditionArgs("vip", A1, context))) { }
        /// </code>
        /// </example>
        public ConditionArgs ConditionArgs(string name, TokenAttributeDictionary attributes, object? context) => this.run.ConditionArgs(name, attributes, context);

        /// <summary>
        /// Makes the args for a FOREACH or REUSE_FOREACH tag.
        /// </summary>
        /// <param name="name">The lower case name of the loop.</param>
        /// <param name="attributes">The attributes of the FOREACH tag.</param>
        /// <param name="context">The current context.</param>
        /// <returns>New args; a provider may keep them.</returns>
        /// <example>
        /// <code>
        /// LoopArgs args = run.LoopArgs("lines", A2, context);
        /// </code>
        /// </example>
        public LoopArgs LoopArgs(string name, TokenAttributeDictionary attributes, object? context) => new LoopArgs(name, context, this.run.ParentContext, attributes);

        /// <summary>
        /// Makes the args for a PROCESS_TEMPLATE tag.
        /// </summary>
        /// <param name="name">The lower case name of the sub template.</param>
        /// <param name="attributes">The attributes of the tag.</param>
        /// <param name="context">The current context.</param>
        /// <returns>New args; a provider may keep them.</returns>
        /// <example>
        /// <code>
        /// ProcessTemplateArgs args = run.ProcessTemplateArgs("footer", A3, context);
        /// </code>
        /// </example>
        public ProcessTemplateArgs ProcessTemplateArgs(string name, TokenAttributeDictionary attributes, object? context) => new ProcessTemplateArgs(name, attributes, context, this.run.ParentContext);

        /// <summary>
        /// Gets an empty builder for the value of a SET block or for a sub template that is indented. Give it back with <see cref="ReturnBuilder"/>.
        /// </summary>
        /// <returns>An empty builder.</returns>
        /// <example>
        /// <code>
        /// var value = run.RentBuilder();
        /// value.Append("x");
        /// scope.Set("name", value.ToString());
        /// run.ReturnBuilder(value);
        /// </code>
        /// </example>
        public StringBuilder RentBuilder() => this.run.RentBuilder();

        /// <summary>
        /// Gives back a builder from <see cref="RentBuilder"/>. Give builders back in the reverse order you got them.
        /// </summary>
        /// <param name="builder">The builder. It is cleared.</param>
        /// <example>
        /// <code>
        /// run.ReturnBuilder(value);
        /// </code>
        /// </example>
        public void ReturnBuilder(StringBuilder builder) => this.run.ReturnBuilder(builder);

        /// <summary>
        /// Starts a FOREACH. Call <see cref="ExitLoop"/> when it ends.
        /// </summary>
        /// <example>
        /// <code>
        /// run.EnterLoop();
        /// </code>
        /// </example>
        public void EnterLoop() => this.run.EnterLoop();

        /// <summary>
        /// Marks the start of a row of the innermost FOREACH.
        /// </summary>
        /// <param name="isLast">True for the last row.</param>
        /// <example>
        /// <code>
        /// run.SetRow(i == list.Count - 1);
        /// </code>
        /// </example>
        public void SetRow(bool isLast) => this.run.SetRow(isLast);

        /// <summary>
        /// Marks that the rows of the innermost FOREACH are done, before its FOOTER.
        /// </summary>
        /// <example>
        /// <code>
        /// run.LeaveRow();
        /// </code>
        /// </example>
        public void LeaveRow() => this.run.LeaveRow();

        /// <summary>
        /// Ends the innermost FOREACH.
        /// </summary>
        /// <example>
        /// <code>
        /// run.ExitLoop();
        /// </code>
        /// </example>
        public void ExitLoop() => this.run.ExitLoop();

        /// <summary>
        /// Removes the last entry equal to <paramref name="value"/> from <see cref="ParentContext"/>: the one the block added.
        /// </summary>
        /// <param name="value">The object the block added.</param>
        /// <example>
        /// <code>
        /// run.ParentContext.Add(row);
        /// // ... the row ...
        /// run.RemoveParent(row);
        /// </code>
        /// </example>
        public void RemoveParent(object? value) => this.run.RemoveParent(value);

        /// <summary>
        /// Does <c>&lt;%REMOVE_PREVIOUS n%&gt;</c>: removes up to <paramref name="count"/> chars at the end, but only text this run wrote.
        /// </summary>
        /// <param name="output">The builder being written.</param>
        /// <param name="count">The number of chars.</param>
        /// <example>
        /// <code>
        /// run.RemovePrevious(output, 1);
        /// </code>
        /// </example>
        public void RemovePrevious(StringBuilder output, int count) => this.run.RemovePrevious(output, count);

        /// <summary>
        /// Does <c>&lt;%REMOVE_PREVIOUS_NEW_LINE%&gt;</c>: removes one line break at the end, but only text this run wrote.
        /// </summary>
        /// <param name="output">The builder being written.</param>
        /// <example>
        /// <code>
        /// run.RemovePreviousNewLine(output);
        /// </code>
        /// </example>
        public void RemovePreviousNewLine(StringBuilder output) => this.run.RemovePreviousNewLine(output);
    }
}
