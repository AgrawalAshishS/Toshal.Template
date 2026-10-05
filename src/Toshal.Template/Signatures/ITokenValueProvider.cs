// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Signatures
{
    /// <summary>
    /// The shape of a class that gives the text of <c>&lt;%=name%&gt;</c> tags. Its method matches <see cref="Processor.TokenValueProvider"/>.
    /// </summary>
    /// <remarks>
    /// <para>The <see cref="Processor"/> does not use this interface; it has delegate properties. The interface is a ready made shape for a provider class:
    /// implement it, then assign the method to the matching property of the processor.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// processor.TokenValueProvider = myProvider.TokenValueProvider;
    /// </code>
    /// </example>
    public interface ITokenValueProvider
    {
        /// <summary>
        /// Gives the text for the named value tag.
        /// </summary>
        /// <param name="args">The name, attributes and contexts of the tag.</param>
        /// <returns>The text to write. Null or an empty string writes nothing.</returns>
        /// <example>
        /// <code>
        /// public string TokenValueProvider(TokenArgs args) =&gt; ((Customer)args.Context!).Name;
        /// </code>
        /// </example>
        string TokenValueProvider(TokenArgs args);
    }
}
