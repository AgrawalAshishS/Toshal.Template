// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

using System.Collections.Generic;

namespace Toshal.Template
{
    /// <summary>
    /// The common part of the arguments that the <see cref="Processor"/> passes to its value providers:
    /// the name of the tag, the current context and the contexts around it.
    /// </summary>
    /// <example>
    /// <code>
    /// processor.TokenValueProvider = args =&gt; args.Name == "name" ? ((Customer)args.Context!).Name : null;
    /// </code>
    /// </example>
    public abstract class ArgsBase
    {
        /// <summary>
        /// Sets the name, the context and the parent contexts. Only the derived argument classes call it.
        /// </summary>
        /// <param name="name">The name of the tag, in lower case. For <c>&lt;%=Name%&gt;</c> it is <c>name</c>.</param>
        /// <param name="context">The current context object. It can be null.</param>
        /// <param name="parentContext">The list of contexts around the tag, outermost first. It is stored as is, not copied.</param>
        /// <example>
        /// <code>
        /// var args = new TokenArgs(token, customer, new List&lt;object?&gt; { customer });
        /// </code>
        /// </example>
        protected ArgsBase(string name, object? context, List<object?> parentContext)
        {
            this.Name = name;
            this.Context = context;
            ParentContext = parentContext;
        }

        /// <summary>
        /// Gets the current context: the object that <see cref="ProcessorArgs.Context"/> set at the top, the current item inside
        /// a FOREACH row, the list itself inside HEADER and FOOTER, or the value that the with value provider returned inside WITH.
        /// </summary>
        /// <example>
        /// <code>
        /// var customer = (Customer)args.Context!;
        /// </code>
        /// </example>
        public object? Context { get; private set; }

        /// <summary>
        /// Gets the name of the tag in lower case. The parser lower cases every name, so <c>&lt;%=FirstName%&gt;</c> gives <c>firstname</c>.
        /// </summary>
        /// <remarks>
        /// <para><b>Warning:</b> compare with lower case names, for example <c>args.Name == "firstname"</c>.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// switch (args.Name) { case "firstname": return customer.FirstName; }
        /// </code>
        /// </example>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the contexts around the tag, outermost first. The processor adds the top context (when it is not null),
        /// then for each FOREACH the list and the current item, and for each WITH its value.
        /// The current context is normally the last entry.
        /// </summary>
        /// <remarks>
        /// <para><b>Warning:</b> this is the live list that the processor uses as a stack. It changes after the provider returns.
        /// Do not change it, and copy it if you need to keep it.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Inside a FOREACH of orders inside a WITH customer: find the customer.
        /// var customer = args.ParentContext.OfType&lt;Customer&gt;().LastOrDefault();
        /// </code>
        /// </example>
        public List<object?> ParentContext { get; private set; }
    }
}
