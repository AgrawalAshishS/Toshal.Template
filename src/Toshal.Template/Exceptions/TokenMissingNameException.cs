// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Exceptions
{
    /// <summary>
    /// Thrown by <see cref="Parser.Parse(string)"/> when a tag that needs a name has none,
    /// for example <c>&lt;%=%&gt;</c>, <c>&lt;%IF %&gt;</c> or <c>&lt;%FOREACH %&gt;</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// Assert.Throws&lt;TokenMissingNameException&gt;(() =&gt; new Parser().Parse("&lt;%=%&gt;"));
    /// </code>
    /// </example>
    public class TokenMissingNameException : ParserException
    {
        /// <summary>
        /// Creates the exception for the tag without a name.
        /// </summary>
        /// <param name="split">The tag. Its line and column are copied.</param>
        /// <example>
        /// <code>
        /// throw new TokenMissingNameException(split);
        /// </code>
        /// </example>
        public TokenMissingNameException(Split split)
            : base(split)
        {
        }
    }
}
