// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

using System;

namespace Toshal.Template.Tokens
{
    /// <summary>
    /// A parsed <c>&lt;%CONTEXT_AS_STRING%&gt;</c> tag. The processor writes <c>ToString()</c> of the current context, or nothing when the context is null.
    /// It needs no provider, so it is handy for a list of strings or numbers.
    /// </summary>
    /// <example>
    /// <code>
    /// // With a loop provider that returns new[] { "a", "b" }:
    /// // &lt;%FOREACH tags%&gt;#&lt;%CONTEXT_AS_STRING%&gt; &lt;%ENDFOR%&gt;   writes   #a #b
    /// </code>
    /// </example>
    public sealed class ContextAsStringToken : Token
    {
        /// <summary>
        /// Creates the token. The parser calls it.
        /// </summary>
        /// <param name="split">The tag. Its line and column are copied.</param>
        /// <example>
        /// <code>
        /// var token = new ContextAsStringToken(new Split { Content = "&lt;%CONTEXT_AS_STRING%&gt;" });
        /// </code>
        /// </example>
        public ContextAsStringToken(Split split)
        {
            this.StartingPosition = split.StartingPosition;
            this.LineNumber = split.LineNumber;
        }
    }
}
