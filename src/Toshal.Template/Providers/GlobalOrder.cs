// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Providers
{
    /// <summary>
    /// When a <see cref="GlobalProvider"/> is asked, compared with the providers for the type of the context.
    /// </summary>
    /// <example>
    /// <code>
    /// registry.RegisterGlobal(new ProjectGlobals(), GlobalOrder.BeforeTyped);
    /// </code>
    /// </example>
    public enum GlobalOrder
    {
        /// <summary>
        /// Asked after the typed providers, only when none of them handled the name. Use it for fallbacks.
        /// </summary>
        AfterTyped = 0,

        /// <summary>
        /// Asked before the typed providers. Its answer wins. It runs for every tag, so keep it short.
        /// </summary>
        BeforeTyped = 1,
    }
}
