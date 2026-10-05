// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Tokens
{
    /// <summary>
    /// The <c>&lt;%ELSE%&gt;</c> part of an IF block. It is the <see cref="ConditionToken.FalsePart"/> of the last IF or ELSEIF.
    /// </summary>
    /// <example>
    /// <code>
    /// var token = (ConditionToken)new Parser().Parse("&lt;%IF paid%&gt;Thanks&lt;%ELSE%&gt;Please pay&lt;%ENDIF%&gt;")[0];
    /// var elsePart = (ElseToken)token.FalsePart!;   // elsePart.InnerTokens holds "Please pay"
    /// </code>
    /// </example>
    public class ElseToken : ContainerTokenBase
    {
        /// <summary>
        /// Creates the token. The parser calls it and then fills <see cref="ContainerTokenBase.InnerTokens"/>.
        /// </summary>
        /// <param name="split">The ELSE tag. Its line and column are copied.</param>
        /// <example>
        /// <code>
        /// var token = new ElseToken(new Split { Content = "&lt;%ELSE%&gt;" });
        /// </code>
        /// </example>
        public ElseToken(Split split)
        {
            this.LineNumber = split.LineNumber;
            this.StartingPosition = split.StartingPosition;
        }
    }
}
