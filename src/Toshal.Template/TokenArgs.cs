// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template
{
    using System.Collections.Generic;
    using Template.Tokens;

    /// <summary>
    /// The arguments of <see cref="Processor.TokenValueProvider"/> for a <c>&lt;%=name%&gt;</c> tag, and of
    /// <see cref="Processor.WithValueProvider"/> for a <c>&lt;%WITH name%&gt;</c> tag.
    /// </summary>
    /// <example>
    /// <code>
    /// processor.TokenValueProvider = args =&gt; args.Name switch
    /// {
    ///     "total" =&gt; ((Order)args.Context!).Total.ToString(args.GetAttribute("format", "0.00")),
    ///     _ =&gt; null,
    /// };
    /// </code>
    /// </example>
    public class TokenArgs : ArgsBase
    {
        /// <summary>
        /// Creates the arguments for a value tag. The processor calls it; you need it only to test a provider on its own.
        /// </summary>
        /// <param name="token">The parsed value tag. Its name and attributes are copied.</param>
        /// <param name="context">The current context. It can be null.</param>
        /// <param name="parentContext">The contexts around the tag, outermost first. It is stored as is, not copied.</param>
        /// <example>
        /// <code>
        /// var token = (NamedToken)new Parser().Parse("&lt;%=total%&gt;")[0];
        /// string? text = myProvider(new TokenArgs(token, order, new List&lt;object?&gt; { order }));
        /// </code>
        /// </example>
        public TokenArgs(NamedToken token, object? context, List<object?> parentContext)
            : base(token.Name, context, parentContext)
        {
            this.Attributes = token.Attributes;
        }

        /// <summary>
        /// Creates the arguments for a WITH tag. The processor calls it; you need it only to test a provider on its own.
        /// </summary>
        /// <param name="token">The parsed WITH tag. Its name and attributes are copied.</param>
        /// <param name="context">The current context. It can be null.</param>
        /// <param name="parentContext">The contexts around the tag, outermost first. It is stored as is, not copied.</param>
        /// <example>
        /// <code>
        /// var token = (WithToken)new Parser().Parse("&lt;%WITH customer%&gt;x&lt;%ENDWITH%&gt;")[0];
        /// object? value = myProvider(new TokenArgs(token, order, new List&lt;object?&gt; { order }));
        /// </code>
        /// </example>
        public TokenArgs(WithToken token, object? context, List<object?> parentContext)
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
        /// // "customer.name": hand "name" to the customer provider with the customer as context.
        /// string? name = customerProvider(new TokenArgs(args, order.Customer));
        /// </code>
        /// </example>
        public TokenArgs(TokenArgs args, object? context)
            : base(args.Name, context, new List<object?>(args.ParentContext))
        {
            ParentContext.Add(args.Context);
            this.Attributes = args.Attributes;
        }

        /// <summary>
        /// Gets the attributes written in the tag, for example <c>format</c> in <c>&lt;%=Total format="0.00"%&gt;</c>.
        /// Keys are lower case, values are kept as written.
        /// </summary>
        /// <example>
        /// <code>
        /// bool hasFormat = args.Attributes.ContainsKey("format");
        /// </code>
        /// </example>
        public TokenAttributeDictionary Attributes { get; private set; }

        /// <summary>
        /// Gets an attribute value, or a default when the tag does not have that attribute. The key is not case sensitive.
        /// </summary>
        /// <param name="key">The attribute name.</param>
        /// <param name="defaultValue">The value to return when the attribute is missing.</param>
        /// <returns>The attribute value as written in the tag, or <paramref name="defaultValue"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <example>
        /// <code>
        /// // &lt;%=Total format="0.00"%&gt;
        /// string format = args.GetAttribute("Format", "0");   // "0.00"
        /// </code>
        /// </example>
        public string GetAttribute(string key, string defaultValue)
        {
            return Attributes.GetValue(key, defaultValue);
        }
    }
}
