// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

using System.Collections;

namespace Toshal.Template.Signatures
{
    /// <summary>
    /// The shape of a class that gives the rows of FOREACH and REUSE_FOREACH loops. Its method matches <see cref="Processor.LoopValueProvider"/>.
    /// </summary>
    /// <remarks>
    /// <para>The <see cref="Processor"/> does not use this interface; it has delegate properties. The interface is a ready made shape for a provider class:
    /// implement it, then assign the method to the matching property of the processor.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// processor.LoopValueProvider = myProvider.LoopValueProvider;
    /// </code>
    /// </example>
    public interface ILoopValueProvider
    {
        /// <summary>
        /// Gives the rows of the named loop for the current context.
        /// </summary>
        /// <param name="args">The name, attributes and contexts of the loop tag.</param>
        /// <returns>The rows. Each row becomes the context of one ROW block. Null or an empty list runs the NORECORD block.</returns>
        /// <example>
        /// <code>
        /// public IList LoopValueProvider(LoopArgs args) =&gt; ((Order)args.Context!).Lines;
        /// </code>
        /// </example>
        IList LoopValueProvider(LoopArgs args);
    }
}
