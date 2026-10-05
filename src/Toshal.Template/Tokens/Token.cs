// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Toshal.Template.Tokens
{
    /// <summary>
    /// The base class of all tokens. It holds the position of the token in the template.
    /// </summary>
    /// <example>
    /// <code>
    /// Token token = new ContentToken(new Split { Content = "Hi", LineNumber = 1, StartingPosition = 1 });
    /// </code>
    /// </example>
    public abstract class Token : IToken
    {
        /// <summary>
        /// Gets or sets the 1 based line where the token starts.
        /// </summary>
        /// <example>
        /// <code>
        /// int line = token.LineNumber;
        /// </code>
        /// </example>
        public int LineNumber { get; set; }

        /// <summary>
        /// Gets or sets the 1 based column where the token starts.
        /// </summary>
        /// <example>
        /// <code>
        /// int column = token.StartingPosition;
        /// </code>
        /// </example>
        public int StartingPosition { get; set; }
    }
}
