// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template
{
    using System.Collections.Generic;
    using Toshal.Template.Tokens;

    /// <summary>
    /// The arguments of <see cref="Processor.LoopValueProvider"/> for a <c>&lt;%FOREACH name%&gt;</c> or <c>&lt;%REUSE_FOREACH existing name%&gt;</c> tag.
    /// </summary>
    /// <example>
    /// <code>
    /// processor.LoopValueProvider = args =&gt; args.Name == "lines" ? ((Order)args.Context!).Lines : null;
    /// </code>
    /// </example>
    public class LoopArgs : ArgsBase
    {
        /// <summary>
        /// Creates the arguments with a name and attributes given directly. The processor uses it for REUSE_FOREACH:
        /// the name is the new name and the attributes come from the reused FOREACH.
        /// </summary>
        /// <param name="loopName">The name of the loop, in lower case.</param>
        /// <param name="context">The current context. It can be null.</param>
        /// <param name="parentContext">The contexts around the tag, outermost first. It is stored as is, not copied.</param>
        /// <param name="attributes">The attributes of the loop. It is stored as is, not copied.</param>
        /// <example>
        /// <code>
        /// var args = new LoopArgs("lines", order, new List&lt;object?&gt; { order }, new TokenAttributeDictionary());
        /// </code>
        /// </example>
        public LoopArgs(string loopName, object? context, List<object?> parentContext, TokenAttributeDictionary attributes)
            : base(loopName, context, parentContext)
        {
            this.Attributes = attributes;
        }

        /// <summary>
        /// Creates the arguments for a FOREACH tag. The processor calls it; you need it only to test a provider on its own.
        /// </summary>
        /// <param name="token">The parsed FOREACH tag. Its name and attributes are copied.</param>
        /// <param name="context">The current context. It can be null.</param>
        /// <param name="parentContext">The contexts around the tag, outermost first. It is stored as is, not copied.</param>
        /// <example>
        /// <code>
        /// var token = (ForEachToken)new Parser().Parse("&lt;%FOREACH lines%&gt;x&lt;%ENDFOR%&gt;")[0];
        /// IList? rows = myProvider(new LoopArgs(token, order, new List&lt;object?&gt; { order }));
        /// </code>
        /// </example>
        public LoopArgs(ForEachToken token, object? context, List<object?> parentContext)
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
        /// var child = new LoopArgs(args, order.Customer);   // child.ParentContext ends with order
        /// </code>
        /// </example>
        public LoopArgs(LoopArgs args, object? context)
            : base(args.Name, context, new List<object?>(args.ParentContext))
        {
            ParentContext.Add(args.Context);
            this.Attributes = args.Attributes;
        }

        /// <summary>
        /// Gets the attributes written in the FOREACH tag, for example <c>top</c> in <c>&lt;%FOREACH lines top="5"%&gt;</c>. Keys are lower case.
        /// For REUSE_FOREACH these are the attributes of the reused FOREACH.
        /// </summary>
        /// <remarks>
        /// <para><b>Known issue:</b> in FOREACH tags the values are lower cased too, so <c>sort="Name"</c> gives <c>name</c>. See docs/known-issues.md.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// int top = int.Parse(args.Attributes.GetValue("top", "10"));
        /// </code>
        /// </example>
        public TokenAttributeDictionary Attributes { get; private set; }
    }
}
