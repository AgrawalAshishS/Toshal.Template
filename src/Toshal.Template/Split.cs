// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Toshal.Template
{
    /// <summary>
    /// One piece of the template text as the parser cuts it: either a whole tag such as <c>&lt;%=Name%&gt;</c> or the plain text between tags.
    /// Token constructors read it, and <see cref="Exceptions.ParserException.Split"/> points to the piece that failed.
    /// </summary>
    /// <remarks>
    /// <para>This is a parser detail that is public. It is documented as it is; code outside the library rarely needs it.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var token = new NamedToken(new Split { Content = "&lt;%=Name%&gt;", LineNumber = 1, StartingPosition = 1 });
    /// </code>
    /// </example>
    public class Split
    {
        /// <summary>
        /// The text of the piece, including <c>&lt;%</c> and <c>%&gt;</c> for a tag. The default is an empty string.
        /// </summary>
        /// <example>
        /// <code>
        /// catch (ParserException ex) { Console.WriteLine(ex.Split?.Content); }
        /// </code>
        /// </example>
        public string Content = string.Empty;

        /// <summary>
        /// The 1 based column of the first character of the piece. The default is 0.
        /// </summary>
        /// <example>
        /// <code>
        /// int column = split.StartingPosition;
        /// </code>
        /// </example>
        public int StartingPosition = 0;

        /// <summary>
        /// The 1 based line of the first character of the piece. The default is 0.
        /// </summary>
        /// <example>
        /// <code>
        /// int line = split.LineNumber;
        /// </code>
        /// </example>
        public int LineNumber = 0;
    }
}
