// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Tokens
{
    using System.Collections.Generic;

    /// <summary>
    /// The base class of tokens that hold other tokens: <see cref="ConditionToken"/>, <see cref="ElseToken"/>, <see cref="SetToken"/> and <see cref="WithToken"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// if (token is ContainerTokenBase block) { Console.WriteLine(block.InnerTokens.Count); }
    /// </code>
    /// </example>
    public abstract class ContainerTokenBase : Token, IContainerToken
    {
        /// <summary>
        /// Creates the token with an empty <see cref="InnerTokens"/> list.
        /// </summary>
        /// <example>
        /// <code>
        /// var token = new ElseToken(split);   // token.InnerTokens is empty
        /// </code>
        /// </example>
        protected ContainerTokenBase()
        {
            this.InnerTokens = new List<IToken>();
        }

        /// <summary>
        /// Gets the tokens inside the block, in template order.
        /// </summary>
        /// <example>
        /// <code>
        /// foreach (IToken inner in block.InnerTokens) { /* ... */ }
        /// </code>
        /// </example>
        public List<IToken> InnerTokens { get; }
    }
}
