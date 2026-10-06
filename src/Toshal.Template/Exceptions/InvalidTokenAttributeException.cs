// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Exceptions
{
    /// <summary>
    /// Thrown by <see cref="Parser.Parse(string)"/> when the attributes of a tag are not written as <c>name="value"</c>,
    /// for example <c>&lt;%=Total format=0.00%&gt;</c> or <c>&lt;%=Total format = "0.00"%&gt;</c>, or when a tag has the same attribute twice.
    /// </summary>
    /// <example>
    /// <code>
    /// Assert.Throws&lt;InvalidTokenAttributeException&gt;(() =&gt; new Parser().Parse("&lt;%=Total format=0.00%&gt;"));
    /// </code>
    /// </example>
    public class InvalidTokenAttributeException : ParserException
    {
        /// <summary>
        /// Creates the exception for the tag that has the bad attributes.
        /// </summary>
        /// <param name="split">The tag. Its line and column are copied.</param>
        /// <example>
        /// <code>
        /// throw new InvalidTokenAttributeException(split);
        /// </code>
        /// </example>
        public InvalidTokenAttributeException(Split split)
            : base(split)
        {
        }
    }
}
