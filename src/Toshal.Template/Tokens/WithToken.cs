// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Tokens
{
    using System.Text.RegularExpressions;

    using Toshal.Template.Exceptions;

    /// <summary>
    /// A parsed <c>&lt;%WITH name%&gt;</c> ... <c>&lt;%ENDWITH%&gt;</c> block. The processor asks <see cref="Processor.WithValueProvider"/> for an object,
    /// and that object is the context of the tags inside the block. When the provider returns null the whole block is skipped.
    /// </summary>
    /// <example>
    /// <code>
    /// // &lt;%WITH Customer%&gt;Dear &lt;%=Name%&gt;,&lt;%ENDWITH%&gt;
    /// processor.WithValueProvider = args =&gt; args.Name == "customer" ? ((Order)args.Context!).Customer : null;
    /// </code>
    /// </example>
    public class WithToken : ContainerTokenBase
    {
        /// <summary>
        /// Reads the name and attributes of the tag. The parser calls it and then fills <see cref="ContainerTokenBase.InnerTokens"/>.
        /// </summary>
        /// <param name="split">The WITH tag.</param>
        /// <exception cref="TokenMissingNameException">The tag has no name.</exception>
        /// <exception cref="InvalidTokenAttributeException">The attributes are not written as <c>name="value"</c>.</exception>
        /// <example>
        /// <code>
        /// var token = new WithToken(new Split { Content = "&lt;%WITH customer%&gt;" });
        /// </code>
        /// </example>
        public WithToken(Split split)
        {
            const string withTokenExpression = "<%WITH\\s(?<Name>.+?)%>";

            this.Name = TokenAttributeDictionary.GetNameAndAttributes(
                split,
                Regex.Match(split.Content, withTokenExpression).Groups["Name"].Value.Trim(),
                this.Attributes);

            this.LineNumber = split.LineNumber;
            this.StartingPosition = split.StartingPosition;

            if (string.IsNullOrEmpty(this.Name))
            {
                throw new TokenMissingNameException(split);
            }
        }

        /// <summary>
        /// Gets the attributes of the tag. Keys are lower case, values are kept as written.
        /// </summary>
        /// <example>
        /// <code>
        /// string role = token.Attributes.GetValue("role", "billing");
        /// </code>
        /// </example>
        public TokenAttributeDictionary Attributes { get; } = new TokenAttributeDictionary();

        /// <summary>
        /// Gets the name in lower case, without the attributes.
        /// </summary>
        /// <example>
        /// <code>
        /// string name = token.Name;   // "customer"
        /// </code>
        /// </example>
        public string Name { get; } = string.Empty;
    }
}
