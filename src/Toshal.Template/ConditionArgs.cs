// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template
{
    using System.Collections.Generic;
    using Toshal.Template.Tokens;

    /// <summary>
    /// The arguments of <see cref="Processor.ConditionValueProvider"/> for an <c>&lt;%IF name%&gt;</c> or <c>&lt;%ELSEIF name%&gt;</c> tag.
    /// </summary>
    /// <remarks>
    /// <para>For <c>&lt;%IF not Paid%&gt;</c> the name is <c>paid</c>. Return whether <c>paid</c> is true; the processor applies the <c>not</c>.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// processor.ConditionValueProvider = args =&gt; args.Name == "paid" &amp;&amp; ((Invoice)args.Context!).Paid;
    /// </code>
    /// </example>
    public class ConditionArgs : ArgsBase
    {
        /// <summary>
        /// Creates the arguments for a condition tag. The processor calls it; you need it only to test a provider on its own.
        /// </summary>
        /// <param name="token">The parsed condition tag. Its name and attributes are copied.</param>
        /// <param name="context">The current context. It can be null.</param>
        /// <param name="parentContext">The contexts around the tag, outermost first. It is stored as is, not copied.</param>
        /// <example>
        /// <code>
        /// var token = (ConditionToken)new Parser().Parse("&lt;%IF paid%&gt;x&lt;%ENDIF%&gt;")[0];
        /// bool result = myProvider(new ConditionArgs(token, invoice, new List&lt;object?&gt; { invoice }));
        /// </code>
        /// </example>
        public ConditionArgs(ConditionToken token, object? context, List<object?> parentContext)
            : base(token.Name, context, parentContext)
        {
            this.Attributes = token.Attributes;
        }

        /// <summary>
        /// Creates arguments for a child object: same name and attributes, a new context, and the old context added at the end of a copy of
        /// the parent contexts. Use it when a provider hands part of the work to another provider.
        /// </summary>
        /// <param name="args">The arguments to copy. They are not changed.</param>
        /// <param name="context">The new context. It can be null.</param>
        /// <example>
        /// <code>
        /// var child = new ConditionArgs(args, customer.Address);   // child.ParentContext ends with customer
        /// </code>
        /// </example>
        public ConditionArgs(ConditionArgs args, object? context)
            : base(args.Name, context, new List<object?>(args.ParentContext))
        {
            ParentContext.Add(args.Context);
            this.Attributes = args.Attributes;
        }

        /// <summary>
        /// Gets the attributes written in the tag, for example <c>min</c> in <c>&lt;%IF total min="100"%&gt;</c>. Keys are lower case.
        /// </summary>
        /// <remarks>
        /// <para><b>Known issue:</b> in IF and ELSEIF tags the values are lower cased too, so <c>unit="KG"</c> gives <c>kg</c>. See docs/known-issues.md.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// string min = args.Attributes.GetValue("min", "0");
        /// </code>
        /// </example>
        public TokenAttributeDictionary Attributes { get; private set; }
    }
}
