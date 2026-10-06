// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Providers
{
    using System.Collections;
    using System.Text;

    /// <summary>
    /// Answers names that do not depend on the type of the current context, such as a project name, a version or today's date.
    /// It is asked for every context, also when the context is null. Register it with
    /// <see cref="ContextProviderRegistry.RegisterGlobal(GlobalProvider, GlobalOrder)"/>.
    /// </summary>
    /// <remarks>
    /// <para>Each method returns <c>true</c> when it handled the name, even when the value is null, empty or <c>false</c>.
    /// Return <c>false</c> to let the next provider try.</para>
    /// <para><b>Warning:</b> a global provider runs for every tag of its kind, so keep it short: one <c>switch</c> on <see cref="ArgsBase.Name"/>.
    /// It is called from many threads when you process templates in parallel.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// sealed class ProjectGlobals : GlobalProvider
    /// {
    ///     public override bool TryToken(TokenArgs args, out string? value)
    ///     {
    ///         switch (args.Name)
    ///         {
    ///             case "project_name": value = "Shop"; return true;
    ///         }
    ///
    ///         value = null;
    ///         return false;
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class GlobalProvider
    {
        /// <summary>
        /// Answers <c>&lt;%=name%&gt;</c>. The default answers "not handled".
        /// </summary>
        /// <param name="args">The name, the attributes, the context (it can be null) and the parent contexts of the tag.</param>
        /// <param name="value">The text to write. Null or empty writes nothing.</param>
        /// <returns><c>true</c> when this provider handled the name; <c>false</c> to let the next provider try.</returns>
        /// <example>
        /// <code>
        /// public override bool TryToken(TokenArgs args, out string? value)
        /// {
        ///     switch (args.Name)
        ///     {
        ///         case "current_year": value = DateTime.Now.Year.ToString(); return true;
        ///     }
        ///
        ///     value = null;
        ///     return false;
        /// }
        /// </code>
        /// </example>
        public virtual bool TryToken(TokenArgs args, out string? value)
        {
            value = null;
            return false;
        }

        /// <summary>
        /// Answers <c>&lt;%=name%&gt;</c> by appending the value straight to the output. The default calls <see cref="TryToken"/> and appends its value.
        /// </summary>
        /// <param name="args">The name, the attributes, the context (it can be null) and the parent contexts of the tag.</param>
        /// <param name="output">The text written so far. Append to it.</param>
        /// <returns><c>true</c> when this provider handled the name; <c>false</c> to let the next provider try.</returns>
        /// <remarks>
        /// <para><b>Warning:</b> return <c>false</c> only when you wrote nothing, and never remove text that is already in <paramref name="output"/>.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// public override bool TryWrite(TokenArgs args, StringBuilder output)
        /// {
        ///     switch (args.Name)
        ///     {
        ///         case "current_year": output.Append(DateTime.Now.Year); return true;
        ///     }
        ///
        ///     return base.TryWrite(args, output);
        /// }
        /// </code>
        /// </example>
        public virtual bool TryWrite(TokenArgs args, StringBuilder output)
        {
            if (!this.TryToken(args, out var value)) return false;

            output.Append(value);
            return true;
        }

        /// <summary>
        /// Answers <c>&lt;%IF name%&gt;</c> and <c>&lt;%ELSEIF name%&gt;</c>. The default answers "not handled".
        /// </summary>
        /// <param name="args">The name, the attributes, the context (it can be null) and the parent contexts of the tag.</param>
        /// <param name="value">Whether the condition is true.</param>
        /// <returns><c>true</c> when this provider handled the name, also when <paramref name="value"/> is <c>false</c>;
        /// <c>false</c> to let the next provider try.</returns>
        /// <example>
        /// <code>
        /// public override bool TryCondition(ConditionArgs args, out bool value)
        /// {
        ///     switch (args.Name)
        ///     {
        ///         case "for_each_count": value = args.Context is IList list &amp;&amp; list.Count == 1; return true;
        ///     }
        ///
        ///     value = false;
        ///     return false;
        /// }
        /// </code>
        /// </example>
        public virtual bool TryCondition(ConditionArgs args, out bool value)
        {
            value = false;
            return false;
        }

        /// <summary>
        /// Answers <c>&lt;%FOREACH name%&gt;</c> and <c>&lt;%REUSE_FOREACH existing name%&gt;</c>. The default answers "not handled".
        /// </summary>
        /// <param name="args">The name, the attributes, the context (it can be null) and the parent contexts of the tag.</param>
        /// <param name="value">The rows. Null or an empty list writes the NORECORD part.</param>
        /// <returns><c>true</c> when this provider handled the name; <c>false</c> to let the next provider try.</returns>
        /// <example>
        /// <code>
        /// public override bool TryLoop(LoopArgs args, out IList? value)
        /// {
        ///     switch (args.Name)
        ///     {
        ///         case "servers": value = Settings.Servers; return true;
        ///     }
        ///
        ///     value = null;
        ///     return false;
        /// }
        /// </code>
        /// </example>
        public virtual bool TryLoop(LoopArgs args, out IList? value)
        {
            value = null;
            return false;
        }

        /// <summary>
        /// Answers <c>&lt;%WITH name%&gt;</c>. The default answers "not handled".
        /// </summary>
        /// <param name="args">The name, the attributes, the context (it can be null) and the parent contexts of the tag.</param>
        /// <param name="value">The context inside the block. Null skips the block.</param>
        /// <returns><c>true</c> when this provider handled the name; <c>false</c> to let the next provider try.</returns>
        /// <example>
        /// <code>
        /// public override bool TryWith(TokenArgs args, out object? value)
        /// {
        ///     switch (args.Name)
        ///     {
        ///         case "current_order": value = args.FindParent&lt;Order&gt;(); return true;
        ///     }
        ///
        ///     value = null;
        ///     return false;
        /// }
        /// </code>
        /// </example>
        public virtual bool TryWith(TokenArgs args, out object? value)
        {
            value = null;
            return false;
        }
    }
}
