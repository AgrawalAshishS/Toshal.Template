// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Exceptions
{
    /// <summary>
    /// Thrown by <see cref="Parser.Parse(string)"/> when a block has no end tag: IF without ENDIF, FOREACH without ENDFOR, WITH without ENDWITH,
    /// SET without ENDSET, or a FOREACH part such as <c>&lt;%ROW%&gt;</c> without <c>&lt;%ENDROW%&gt;</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// try { new Parser().Parse("&lt;%IF paid%&gt;Thanks"); }
    /// catch (TokenNotClosedException ex) { Console.WriteLine(ex.TokenName); }   // paid
    /// </code>
    /// </example>
    public class TokenNotClosedException : ParserException
    {
        /// <summary>
        /// Creates the exception with the message "Token named (name) is not closed".
        /// </summary>
        /// <param name="split">The tag that opened the block. Its line and column are copied.</param>
        /// <param name="name">The name of the block, in lower case.</param>
        /// <example>
        /// <code>
        /// throw new TokenNotClosedException(split, "paid");
        /// </code>
        /// </example>
        public TokenNotClosedException(Split split, string name)
            : base(split, "Token named " + name + " is not closed")
        {
            this.TokenName = name;
        }

        /// <summary>
        /// Creates the exception with your own message.
        /// </summary>
        /// <param name="split">The tag that opened the block. Its line and column are copied.</param>
        /// <param name="name">The name of the block, in lower case.</param>
        /// <param name="additionalMessage">The whole message of the exception.</param>
        /// <example>
        /// <code>
        /// throw new TokenNotClosedException(split, "lines", "&lt;%ROW%&gt; not closed for &lt;%FOREACH lines%&gt;");
        /// </code>
        /// </example>
        public TokenNotClosedException(Split split, string name, string additionalMessage)
            : base(split, additionalMessage)
        {
            this.TokenName = name;
        }

        /// <summary>
        /// Gets or sets the name of the block that is not closed, in lower case.
        /// </summary>
        /// <example>
        /// <code>
        /// string name = ex.TokenName;
        /// </code>
        /// </example>
        public string TokenName { get; set; }
    }
}
