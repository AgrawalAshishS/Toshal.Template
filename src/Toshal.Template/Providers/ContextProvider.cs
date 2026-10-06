// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Providers
{
    using System.Collections;

    /// <summary>
    /// Answers the tags for one type of context. Register it with <see cref="ContextProviderRegistry.Register{T}(ContextProvider{T})"/>.
    /// Override only the methods you need; the others answer "not handled".
    /// </summary>
    /// <typeparam name="T">The type of context this provider answers for. It also answers for every type that derives from it or implements it.</typeparam>
    /// <remarks>
    /// <para>Each method returns <c>true</c> when it handled the name, even when the value is null, empty or <c>false</c>.
    /// The registry then stops and uses that value. Return <c>false</c> to let the next provider try.</para>
    /// <para>No reflection is used. Write a <c>switch</c> on <see cref="ArgsBase.Name"/>; the compiler turns a string switch into a fast lookup.</para>
    /// <para><b>Warning:</b> the registry calls the provider from many threads when you process templates in parallel. Do not keep state in it that changes.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// sealed class CustomerProvider : ContextProvider&lt;Customer&gt;
    /// {
    ///     public override bool TryToken(Customer context, TokenArgs args, out string? value)
    ///     {
    ///         switch (args.Name)
    ///         {
    ///             case "name": value = context.Name; return true;
    ///         }
    ///
    ///         value = null;
    ///         return false;
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class ContextProvider<T>
        where T : class
    {
        /// <summary>
        /// Answers <c>&lt;%=name%&gt;</c>. The default answers "not handled".
        /// </summary>
        /// <param name="context">The current context, already of type <typeparamref name="T"/>. Never null.</param>
        /// <param name="args">The name, the attributes and the parent contexts of the tag.</param>
        /// <param name="value">The text to write. Null or empty writes nothing.</param>
        /// <returns><c>true</c> when this provider handled the name; <c>false</c> to let the next provider try.</returns>
        /// <example>
        /// <code>
        /// public override bool TryToken(Customer context, TokenArgs args, out string? value)
        /// {
        ///     switch (args.Name)
        ///     {
        ///         case "name": value = context.Name; return true;
        ///     }
        ///
        ///     value = null;
        ///     return false;
        /// }
        /// </code>
        /// </example>
        public virtual bool TryToken(T context, TokenArgs args, out string? value)
        {
            value = null;
            return false;
        }

        /// <summary>
        /// Answers <c>&lt;%IF name%&gt;</c> and <c>&lt;%ELSEIF name%&gt;</c>. For <c>&lt;%IF not name%&gt;</c> the processor applies the <c>not</c>.
        /// The default answers "not handled".
        /// </summary>
        /// <param name="context">The current context, already of type <typeparamref name="T"/>. Never null.</param>
        /// <param name="args">The name, the attributes and the parent contexts of the tag.</param>
        /// <param name="value">Whether the condition is true.</param>
        /// <returns><c>true</c> when this provider handled the name, also when <paramref name="value"/> is <c>false</c>;
        /// <c>false</c> to let the next provider try.</returns>
        /// <example>
        /// <code>
        /// public override bool TryCondition(Customer context, ConditionArgs args, out bool value)
        /// {
        ///     switch (args.Name)
        ///     {
        ///         case "vip": value = context.IsVip; return true;
        ///     }
        ///
        ///     value = false;
        ///     return false;
        /// }
        /// </code>
        /// </example>
        public virtual bool TryCondition(T context, ConditionArgs args, out bool value)
        {
            value = false;
            return false;
        }

        /// <summary>
        /// Answers <c>&lt;%FOREACH name%&gt;</c> and <c>&lt;%REUSE_FOREACH existing name%&gt;</c>. The default answers "not handled".
        /// </summary>
        /// <param name="context">The current context, already of type <typeparamref name="T"/>. Never null.</param>
        /// <param name="args">The name, the attributes and the parent contexts of the tag.</param>
        /// <param name="value">The rows. Null or an empty list writes the NORECORD part.</param>
        /// <returns><c>true</c> when this provider handled the name; <c>false</c> to let the next provider try.</returns>
        /// <example>
        /// <code>
        /// public override bool TryLoop(Customer context, LoopArgs args, out IList? value)
        /// {
        ///     switch (args.Name)
        ///     {
        ///         case "orders": value = context.Orders; return true;
        ///     }
        ///
        ///     value = null;
        ///     return false;
        /// }
        /// </code>
        /// </example>
        public virtual bool TryLoop(T context, LoopArgs args, out IList? value)
        {
            value = null;
            return false;
        }

        /// <summary>
        /// Answers <c>&lt;%WITH name%&gt;</c>. The default answers "not handled".
        /// </summary>
        /// <param name="context">The current context, already of type <typeparamref name="T"/>. Never null.</param>
        /// <param name="args">The name, the attributes and the parent contexts of the tag.</param>
        /// <param name="value">The context inside the block. Null skips the block.</param>
        /// <returns><c>true</c> when this provider handled the name; <c>false</c> to let the next provider try.</returns>
        /// <example>
        /// <code>
        /// public override bool TryWith(Order context, TokenArgs args, out object? value)
        /// {
        ///     switch (args.Name)
        ///     {
        ///         case "customer": value = context.Customer; return true;
        ///     }
        ///
        ///     value = null;
        ///     return false;
        /// }
        /// </code>
        /// </example>
        public virtual bool TryWith(T context, TokenArgs args, out object? value)
        {
            value = null;
            return false;
        }
    }
}
