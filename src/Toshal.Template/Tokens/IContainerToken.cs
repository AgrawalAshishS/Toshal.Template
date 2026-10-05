// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Tokens
{
    using System.Collections.Generic;

    /// <summary>
    /// A token that holds other tokens. <see cref="ConditionToken.FalsePart"/> has this type.
    /// </summary>
    /// <example>
    /// <code>
    /// IContainerToken? falsePart = conditionToken.FalsePart;
    /// int count = falsePart?.InnerTokens.Count ?? 0;
    /// </code>
    /// </example>
    public interface IContainerToken : IToken
    {
        /// <summary>
        /// Gets the tokens inside the block, in template order.
        /// </summary>
        /// <example>
        /// <code>
        /// foreach (IToken inner in block.InnerTokens) { /* ... */ }
        /// </code>
        /// </example>
        List<IToken> InnerTokens { get; }
    }
}
