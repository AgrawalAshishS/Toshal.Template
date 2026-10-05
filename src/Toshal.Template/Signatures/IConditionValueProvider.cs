// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Signatures
{
    /// <summary>
    /// The shape of a class that answers IF and ELSEIF conditions. Its method matches <see cref="Processor.ConditionValueProvider"/>.
    /// </summary>
    /// <remarks>
    /// <para>The <see cref="Processor"/> does not use this interface; it has delegate properties. The interface is a ready made shape for a provider class:
    /// implement it, then assign the method to the matching property of the processor.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// processor.ConditionValueProvider = myProvider.ConditionValueProvider;
    /// </code>
    /// </example>
    public interface IConditionValueProvider
    {
        /// <summary>
        /// Says whether the named condition is true for the current context.
        /// </summary>
        /// <param name="args">The name, attributes and contexts of the IF or ELSEIF tag.</param>
        /// <returns>True when the condition holds. For <c>&lt;%IF not name%&gt;</c> return the value of <c>name</c>; the processor applies the <c>not</c>.</returns>
        /// <example>
        /// <code>
        /// public bool ConditionValueProvider(ConditionArgs args) =&gt; args.Name == "paid" &amp;&amp; ((Invoice)args.Context!).Paid;
        /// </code>
        /// </example>
        bool ConditionValueProvider(ConditionArgs args);
    }
}
