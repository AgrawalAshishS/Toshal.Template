// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Tokens
{
    using System.Text.RegularExpressions;

    using Toshal.Template.Exceptions;

    /// <summary>
    /// A parsed <c>&lt;%SET name%&gt;</c> ... <c>&lt;%ENDSET%&gt;</c> block. The processor writes the inner tokens into a variable instead of the output.
    /// A later <c>&lt;%=name%&gt;</c> writes the variable, and the variable wins over the token value provider.
    /// </summary>
    /// <remarks>
    /// <para>Where a variable is visible: a SET at the top or inside IF is visible to everything after it.
    /// A SET inside WITH, PROCESS_TEMPLATE, HEADER, FOOTER, NORECORD, BEFOREROW or AFTERROW stays inside that block.
    /// A SET inside a ROW (or FIRSTROW, ALTROW, LASTROW) is visible to the rest of that row, to the AFTERROW part, and to the BEFOREROW and ROW parts
    /// of the later rows of the same loop, but not after the loop.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // &lt;%SET greeting%&gt;Hello &lt;%=Name%&gt;&lt;%ENDSET%&gt;&lt;%=greeting%&gt;, &lt;%=greeting%&gt;!
    /// </code>
    /// </example>
    public sealed class SetToken : ContainerTokenBase
    {
        /// <summary>
        /// Reads the variable name and attributes of the tag. The parser calls it and then fills <see cref="ContainerTokenBase.InnerTokens"/>.
        /// </summary>
        /// <param name="split">The SET tag.</param>
        /// <exception cref="TokenMissingNameException">The tag has no name.</exception>
        /// <exception cref="InvalidTokenAttributeException">The attributes are not written as <c>name="value"</c>.</exception>
        /// <example>
        /// <code>
        /// var token = new SetToken(new Split { Content = "&lt;%SET greeting%&gt;" });
        /// </code>
        /// </example>
        public SetToken(Split split)
        {
            const string tokenExpression = "<%SET\\s(?<Name>.+?)%>";

            this.Name = TokenAttributeDictionary.GetNameAndAttributes(
                split,
                Regex.Match(split.Content, tokenExpression).Groups["Name"].Value.Trim(),
                this.Attributes);

            this.LineNumber = split.LineNumber;
            this.StartingPosition = split.StartingPosition;

            if (string.IsNullOrEmpty(this.Name))
            {
                throw new TokenMissingNameException(split);
            }
        }

        /// <summary>
        /// Gets the variable name in lower case.
        /// </summary>
        /// <example>
        /// <code>
        /// string name = token.Name;   // "greeting"
        /// </code>
        /// </example>
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the attributes of the tag. Keys are lower case. The processor does not use them.
        /// </summary>
        /// <example>
        /// <code>
        /// int count = token.Attributes.Count;
        /// </code>
        /// </example>
        public TokenAttributeDictionary Attributes { get; } = new TokenAttributeDictionary();
    }
}
