// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Tokens
{
    /// <summary>
    /// A parsed <c>&lt;%SEPARATOR%&gt;...&lt;%ENDSEPARATOR%&gt;</c> block. The processor writes its inner part in every row of the innermost
    /// FOREACH except the last one, like the separator of <c>string.Join</c>.
    /// </summary>
    /// <remarks>
    /// <para>It may stand anywhere in a row: in the ROW, ALTROW, FIRSTROW or LASTROW part, in a BEFORE or AFTER part, or inside an IF or WITH in
    /// the row. In a HEADER, FOOTER or NORECORD part, or outside any FOREACH, it writes nothing. It also works in a sub template that a row calls
    /// with PROCESS_TEMPLATE.</para>
    /// <para>Unlike <c>&lt;%REMOVE_PREVIOUS n%&gt;</c> it counts no characters, so it works the same with <c>\n</c> and <c>\r\n</c> line breaks.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // public Customer(
    /// //     int id,
    /// //     string email
    /// // )
    /// var tokens = new Parser().Parse(
    ///     "public Customer(\n    &lt;%FOREACH fields%&gt;\n    &lt;%=type%&gt; &lt;%=name%&gt;&lt;%SEPARATOR%&gt;,&lt;%ENDSEPARATOR%&gt;\n    &lt;%ENDFOR%&gt;\n)\n");
    /// </code>
    /// </example>
    public sealed class SeparatorToken : ContainerTokenBase
    {
        /// <summary>
        /// Creates the token. The parser calls it and then fills <see cref="ContainerTokenBase.InnerTokens"/>.
        /// </summary>
        /// <param name="split">The SEPARATOR tag. Its line and column are copied.</param>
        /// <example>
        /// <code>
        /// var token = new SeparatorToken(new Split { Content = "&lt;%SEPARATOR%&gt;" });
        /// </code>
        /// </example>
        public SeparatorToken(Split split)
        {
            this.LineNumber = split.LineNumber;
            this.StartingPosition = split.StartingPosition;
        }
    }
}
