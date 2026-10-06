// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Tokens
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using Toshal.Template.Exceptions;

    /// <summary>
    /// A parsed <c>&lt;%PROCESS_TEMPLATE name%&gt;</c> tag. The processor asks <see cref="Processor.ProcessTemplateValueProvider"/> for the tokens of
    /// the named sub template and processes them in place, with the current context. The sub template sees the SET variables of the caller,
    /// and its own SET variables stay inside it.
    /// </summary>
    /// <example>
    /// <code>
    /// var token = (ProcessTemplateToken)new Parser().Parse("&lt;%PROCESS_TEMPLATE Footer lang=\"en\"%&gt;")[0];
    /// // token.Name == "footer", token.GetAttribute("lang", "") == "en"
    /// </code>
    /// </example>
    public class ProcessTemplateToken : Token
    {
        /// <summary>
        /// Reads the name and attributes of the tag. The parser calls it.
        /// </summary>
        /// <param name="split">The tag.</param>
        /// <exception cref="TokenMissingNameException">The tag has no name.</exception>
        /// <exception cref="InvalidTokenAttributeException">The attributes are not written as <c>name="value"</c>.</exception>
        /// <example>
        /// <code>
        /// var token = new ProcessTemplateToken(new Split { Content = "&lt;%PROCESS_TEMPLATE footer%&gt;" });
        /// </code>
        /// </example>
        public ProcessTemplateToken(Split split)
        {
            const string processTokenExpression = "<%PROCESS_TEMPLATE\\s(?<Name>.*?)%>";
            string tempString = Regex.Match(split.Content, processTokenExpression).Groups["Name"].Value.Trim();

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
        /// string lang = token.Attributes.GetValue("lang", "en");
        /// </code>
        /// </example>
        public TokenAttributeDictionary Attributes { get; private set; } = new TokenAttributeDictionary();

        /// <summary>
        /// Gets the sub template name in lower case, without the attributes.
        /// </summary>
        /// <example>
        /// <code>
        /// string name = token.Name;
        /// </code>
        /// </example>
        public string Name { get; } = string.Empty;

        /// <summary>
        /// Gets an attribute value, or a default when the tag does not have that attribute. The name is not case sensitive.
        /// </summary>
        /// <param name="attributeName">The attribute name.</param>
        /// <param name="defaultValue">The value to return when the attribute is missing.</param>
        /// <returns>The attribute value as written in the tag, or <paramref name="defaultValue"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="attributeName"/> is null.</exception>
        /// <example>
        /// <code>
        /// string lang = token.GetAttribute("lang", "en");
        /// </code>
        /// </example>
        public string GetAttribute(string attributeName, string defaultValue)
        {
            return Attributes.GetValue(attributeName, defaultValue);
        }
    }
}
