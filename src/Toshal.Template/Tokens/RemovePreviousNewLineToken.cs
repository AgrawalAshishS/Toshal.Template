// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Tokens
{
    /// <summary>
    /// A parsed <c>&lt;%REMOVE_PREVIOUS_NEW_LINE%&gt;</c> tag. The processor removes a <c>\n</c> at the end of the text written so far,
    /// and then a <c>\r</c> at the end. It lets you keep each tag on its own line in the template without empty lines in the output.
    /// </summary>
    /// <remarks>
    /// <para><b>Warning:</b> it also removes a lone <c>\r</c> at the end, even when no <c>\n</c> came after it.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // "Hello\r\n&lt;%REMOVE_PREVIOUS_NEW_LINE%&gt;World"   writes   HelloWorld
    /// </code>
    /// </example>
    public class RemovePreviousNewLineToken : Token
    {
        /// <summary>
        /// Creates the token. The parser calls it.
        /// </summary>
        /// <param name="split">The tag. Its line and column are copied.</param>
        /// <example>
        /// <code>
        /// var token = new RemovePreviousNewLineToken(new Split { Content = "&lt;%REMOVE_PREVIOUS_NEW_LINE%&gt;" });
        /// </code>
        /// </example>
        public RemovePreviousNewLineToken(Split split)
        {
            this.LineNumber = split.LineNumber;
            this.StartingPosition = split.StartingPosition;
        }
    }
}
