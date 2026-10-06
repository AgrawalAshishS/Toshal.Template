// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Tokens
{
    using System;

    using Toshal.Template.Exceptions;

    /// <summary>
    /// A parsed <c>&lt;%=name%&gt;</c> value tag, with optional attributes such as <c>&lt;%=Total format="0.00"%&gt;</c>.
    /// The processor writes the value of a SET variable with this name, or else the text from <see cref="Processor.TokenValueProvider"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// var token = (NamedToken)new Parser().Parse("&lt;%=Total format=\"0.00\"%&gt;")[0];
    /// // token.Name == "total", token.GetAttribute("format", "") == "0.00"
    /// </code>
    /// </example>
    public sealed class NamedToken : Token
    {
        /// <summary>
        /// Reads the name and attributes of a value tag. The parser calls it.
        /// </summary>
        /// <param name="split">The tag.</param>
        /// <exception cref="TokenMissingNameException">The tag has no name, for example <c>&lt;%=%&gt;</c>.</exception>
        /// <exception cref="InvalidTokenAttributeException">The attributes are not written as <c>name="value"</c>.</exception>
        /// <example>
        /// <code>
        /// var token = new NamedToken(new Split { Content = "&lt;%=Name%&gt;" });
        /// </code>
        /// </example>
        public NamedToken(Split split)
        {
            string tempString = TagText.Name(split.Content, "<%=", space: false, minLength: 0).Trim();

            this.Name = TokenAttributeDictionary.GetNameAndAttributes(split, tempString, this.Attributes);
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
        /// bool hasFormat = token.Attributes.ContainsKey("format");
        /// </code>
        /// </example>
        public TokenAttributeDictionary Attributes { get; private set; } = new TokenAttributeDictionary();

        /// <summary>
        /// Gets the name in lower case, without the attributes.
        /// </summary>
        /// <example>
        /// <code>
        /// string name = token.Name;   // "firstname" for &lt;%=FirstName%&gt;
        /// </code>
        /// </example>
        public string Name { get; private set; }

        /// <summary>
        /// Gets an attribute value, or a default when the tag does not have that attribute. The name is not case sensitive.
        /// </summary>
        /// <param name="attributeName">The attribute name.</param>
        /// <param name="defaultValue">The value to return when the attribute is missing.</param>
        /// <returns>The attribute value as written in the tag, or <paramref name="defaultValue"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="attributeName"/> is null.</exception>
        /// <example>
        /// <code>
        /// string format = token.GetAttribute("format", "0");
        /// </code>
        /// </example>
        public string GetAttribute(string attributeName, string defaultValue)
        {
            return Attributes.GetValue(attributeName, defaultValue);
        }
    }
}
