// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Signatures
{
    /// <summary>
    /// The shape of a class that gives the context of WITH blocks. Its method matches <see cref="Processor.WithValueProvider"/>.
    /// </summary>
    /// <remarks>
    /// <para>The <see cref="Processor"/> does not use this interface; it has delegate properties. The interface is a ready made shape for a provider class:
    /// implement it, then assign the method to the matching property of the processor.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// processor.WithValueProvider = myProvider.WithValueProvider;
    /// </code>
    /// </example>
    public interface IWithValueProvider
    {
        /// <summary>
        /// Gives the object that becomes the context inside the named WITH block.
        /// </summary>
        /// <param name="args">The name, attributes and contexts of the WITH tag.</param>
        /// <returns>The new context. Null skips the whole block.</returns>
        /// <example>
        /// <code>
        /// public object WithValueProvider(TokenArgs args) =&gt; ((Order)args.Context!).Customer;
        /// </code>
        /// </example>
        object WithValueProvider(TokenArgs args);
    }
}
