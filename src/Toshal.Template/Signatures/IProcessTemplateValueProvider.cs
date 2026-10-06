// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

using System.Collections.Generic;
using Toshal.Template.Tokens;

namespace Toshal.Template.Signatures
{
    /// <summary>
    /// The shape of a class that gives the sub templates of PROCESS_TEMPLATE tags. Its method matches <see cref="Processor.ProcessTemplateValueProvider"/>.
    /// </summary>
    /// <remarks>
    /// <para>The <see cref="Processor"/> does not use this interface; it has delegate properties. The interface is a ready made shape for a provider class:
    /// implement it, then assign the method to the matching property of the processor.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// processor.ProcessTemplateValueProvider = myProvider.ProcessTemplateValueProvider;
    /// </code>
    /// </example>
    public interface IProcessTemplateValueProvider
    {
        /// <summary>
        /// Gives the parsed sub template for the named PROCESS_TEMPLATE tag.
        /// </summary>
        /// <param name="args">The name, attributes and contexts of the tag.</param>
        /// <returns>The tokens of the sub template, from <see cref="Parser.Parse(string)"/>. Null writes nothing.</returns>
        /// <example>
        /// <code>
        /// public List&lt;IToken&gt;? ProcessTemplateValueProvider(ProcessTemplateArgs args) =&gt; cache.GetValueOrDefault(args.Name);
        /// </code>
        /// </example>
        List<IToken>? ProcessTemplateValueProvider(ProcessTemplateArgs args);
    }
}
