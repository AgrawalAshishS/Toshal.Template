// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template
{
    using System.Collections.Generic;

    using Toshal.Template.Tokens;

    /// <summary>
    /// The input of <see cref="Processor.Process(ProcessorArgs)"/>: the parsed template and the top context object.
    /// </summary>
    /// <example>
    /// <code>
    /// var args = new ProcessorArgs(new Parser().Parse(templateText)) { Context = order };
    /// string text = processor.Process(args).ToString();
    /// </code>
    /// </example>
    public class ProcessorArgs
    {
        /// <summary>
        /// Creates the input for one run of the processor.
        /// </summary>
        /// <param name="tokenList">The tokens that <see cref="Parser.Parse(string)"/> returned. They are not changed, so the same list can be processed many times.</param>
        /// <example>
        /// <code>
        /// List&lt;IToken&gt; tokens = new Parser().Parse("Hello &lt;%=Name%&gt;");
        /// var args = new ProcessorArgs(tokens);
        /// </code>
        /// </example>
        public ProcessorArgs(List<IToken> tokenList)
        {
            this.TokenList = tokenList;
        }

        /// <summary>
        /// Gets or sets the top context object. The providers get it as <see cref="ArgsBase.Context"/> for tags outside any FOREACH or WITH.
        /// When it is not null it is also the first entry of <see cref="ArgsBase.ParentContext"/>. The default is null.
        /// </summary>
        /// <example>
        /// <code>
        /// var args = new ProcessorArgs(tokens) { Context = customer };
        /// </code>
        /// </example>
        public object? Context { get; set; }

        /// <summary>
        /// Gets the tokens to process.
        /// </summary>
        /// <example>
        /// <code>
        /// int count = args.TokenList.Count;
        /// </code>
        /// </example>
        public List<IToken> TokenList { get; private set; }
    }
}
