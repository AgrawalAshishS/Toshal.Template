// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

using System;

namespace Toshal.Template.Tokens
{
    /// <summary>
    /// Plain text of the template, outside any tag. The processor writes it as it is.
    /// </summary>
    /// <example>
    /// <code>
    /// var text = (ContentToken)new Parser().Parse("Hello &lt;%=Name%&gt;")[0];   // text.Content == "Hello "
    /// </code>
    /// </example>
    public class ContentToken : Token
    {
        /// <summary>
        /// Creates the token from a piece of plain text. The parser calls it.
        /// </summary>
        /// <param name="split">The text piece. Its content, line and column are copied.</param>
        /// <example>
        /// <code>
        /// var token = new ContentToken(new Split { Content = "Hello " });
        /// </code>
        /// </example>
        public ContentToken(Split split)
        {
            this.Content = split.Content;
            this.StartingPosition = split.StartingPosition;
            this.LineNumber = split.LineNumber;
        }

        /// <summary>
        /// Gets the text, including spaces and line breaks.
        /// </summary>
        /// <example>
        /// <code>
        /// Console.Write(token.Content);
        /// </code>
        /// </example>
        public string Content { get; private set; }
    }
}
