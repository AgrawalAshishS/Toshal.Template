// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Compiled
{
    using System.ComponentModel;
    using System.Text;

    using Toshal.Template.Processing;

    /// <summary>
    /// The SET variables of one block of a compiled template. Generated code uses it; you do not need it in your own code.
    /// </summary>
    /// <remarks>
    /// <para>A child scope sees the variables of its parent and copies them only when a SET in it writes, the same way as <see cref="Processor"/>.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Generated code for &lt;%WITH customer%&gt;...&lt;%ENDWITH%&gt;:
    /// var withScope = TemplateScope.Child(scope);
    /// </code>
    /// </example>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct TemplateScope
    {
        private TemplateVariables variables;

        /// <summary>
        /// Makes a scope for an inner block. It sees the variables of <paramref name="parent"/>; a SET in it is not seen by the parent.
        /// </summary>
        /// <param name="parent">The scope of the outer block.</param>
        /// <returns>The new scope. It allocates nothing until a SET writes.</returns>
        /// <example>
        /// <code>
        /// var rowScope = TemplateScope.Child(scope);
        /// </code>
        /// </example>
        public static TemplateScope Child(in TemplateScope parent) => new TemplateScope { variables = TemplateVariables.Child(parent.variables) };

        /// <summary>
        /// Appends the value of the SET variable <paramref name="name"/> when there is one. A SET variable wins over the value provider.
        /// </summary>
        /// <param name="output">The builder to append to.</param>
        /// <param name="name">The lower case variable name.</param>
        /// <returns>True when the variable exists and was appended; false when the provider must be asked.</returns>
        /// <example>
        /// <code>
        /// if (!scope.TryAppend(output, "title")) { /* ask TokenValue */ }
        /// </code>
        /// </example>
        public readonly bool TryAppend(StringBuilder output, string name)
        {
            if (this.variables.TryGet(name, out var value))
            {
                output.Append(value);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets a variable, as <c>&lt;%SET name%&gt;value&lt;%ENDSET%&gt;</c> does.
        /// </summary>
        /// <param name="name">The lower case variable name.</param>
        /// <param name="value">The text of the SET block.</param>
        /// <example>
        /// <code>
        /// scope.Set("title", "Invoice");
        /// </code>
        /// </example>
        public void Set(string name, string value) => this.variables.Set(name, value);
    }
}
