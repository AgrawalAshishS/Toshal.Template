// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.


using Toshal.Template.Exceptions;

namespace Toshal.Template.Tokens
{
    /// <summary>
    /// A parsed <c>&lt;%REMOVE_PREVIOUS n%&gt;</c> tag. The processor removes the last n characters written so far, or all of them when fewer were written.
    /// It is useful to drop a trailing separator, such as the last comma of a list.
    /// </summary>
    /// <example>
    /// <code>
    /// // &lt;%FOREACH tags%&gt;&lt;%CONTEXT_AS_STRING%&gt;, &lt;%ENDFOR%&gt;&lt;%REMOVE_PREVIOUS 2%&gt;   writes   a, b
    /// </code>
    /// </example>
    public sealed class RemovePreviousCharsToken : Token
    {
        /// <summary>
        /// Reads the number of characters from the tag. The parser calls it.
        /// </summary>
        /// <param name="split">The tag.</param>
        /// <exception cref="ParserException">The number is missing, is not a whole number, or is negative.</exception>
        /// <example>
        /// <code>
        /// var token = new RemovePreviousCharsToken(new Split { Content = "&lt;%REMOVE_PREVIOUS 2%&gt;" });   // token.CharCount == 2
        /// </code>
        /// </example>
        public RemovePreviousCharsToken(Split split)
        {
            string tempString = TagText.Name(split.Content, "<%REMOVE_PREVIOUS ", space: false, minLength: 0).Trim();
            int charCount = 0;
            if(int.TryParse(tempString, out charCount) == false)
            {
                throw new ParserException(split, "Char count is missing or not integer");
            }

            if (charCount < 0)
            {
                throw new ParserException(split, "Char count must not be negative");
            }

            this.CharCount = charCount;
            this.LineNumber = split.LineNumber;
            this.StartingPosition = split.StartingPosition;
        }

        /// <summary>
        /// Gets the number of characters to remove.
        /// </summary>
        /// <example>
        /// <code>
        /// int count = token.CharCount;
        /// </code>
        /// </example>
        public int CharCount { get; private set; }
    }
}
