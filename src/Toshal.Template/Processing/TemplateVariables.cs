// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Processing
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    // The SET variables of one scope. A child scope shares the dictionary of its parent and copies it only when a SET writes,
    // so blocks without SET allocate nothing. The parent never runs while a child scope is in use, so the shared dictionary cannot change under it.
    internal struct TemplateVariables
    {
        private Dictionary<string, string>? map;
        private bool owned;

        public static TemplateVariables Child(in TemplateVariables parent) => new TemplateVariables { map = parent.map };

        public readonly bool TryGet(string name, [NotNullWhen(true)] out string? value)
        {
            if (this.map == null)
            {
                value = null;
                return false;
            }

            return this.map.TryGetValue(name, out value);
        }

        public void Set(string name, string value)
        {
            if (!this.owned)
            {
                this.map = this.map == null ? new Dictionary<string, string>() : new Dictionary<string, string>(this.map);
                this.owned = true;
            }

            // owned is set only together with a new map.
            this.map![name] = value;
        }
    }
}
