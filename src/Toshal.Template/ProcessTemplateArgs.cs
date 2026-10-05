// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template
{
    using System.Collections.Generic;
    using Template.Tokens;

    /// <summary>
    /// The arguments of <see cref="Processor.ProcessTemplateValueProvider"/> for a <c>&lt;%PROCESS_TEMPLATE name%&gt;</c> tag.
    /// </summary>
    /// <example>
    /// <code>
    /// processor.ProcessTemplateValueProvider = args =&gt; parser.Parse(File.ReadAllText(args.Name + ".txt"));
    /// </code>
    /// </example>
    public class ProcessTemplateArgs : ArgsBase
    {
        /// <summary>
        /// Creates the arguments for a PROCESS_TEMPLATE tag. The processor calls it; you need it only to test a provider on its own.
        /// </summary>
        /// <param name="token">The parsed tag. Its name and attributes are copied.</param>
        /// <param name="context">The current context. It can be null.</param>
        /// <param name="parentContext">The contexts around the tag, outermost first. It is stored as is, not copied.</param>
        /// <example>
        /// <code>
        /// var token = (ProcessTemplateToken)new Parser().Parse("&lt;%PROCESS_TEMPLATE footer%&gt;")[0];
        /// var tokens = myProvider(new ProcessTemplateArgs(token, null, new List&lt;object?&gt;()));
        /// </code>
        /// </example>
        public ProcessTemplateArgs(ProcessTemplateToken token, object? context, List<object?> parentContext)
            : base(token.Name, context, parentContext)
        {
            this.Attributes = token.Attributes;
        }

        /// <summary>
        /// Gets the attributes written in the tag, for example <c>lang</c> in <c>&lt;%PROCESS_TEMPLATE footer lang="en"%&gt;</c>.
        /// Keys are lower case, values are kept as written.
        /// </summary>
        /// <example>
        /// <code>
        /// string lang = args.Attributes.GetValue("lang", "en");
        /// </code>
        /// </example>
        public TokenAttributeDictionary Attributes { get; private set; }

        /// <summary>
        /// Gets an attribute value, or a default when the tag does not have that attribute. The key is not case sensitive.
        /// </summary>
        /// <param name="key">The attribute name. Must not be null.</param>
        /// <param name="defaultValue">The value to return when the attribute is missing.</param>
        /// <returns>The attribute value as written in the tag, or <paramref name="defaultValue"/>.</returns>
        /// <example>
        /// <code>
        /// string lang = args.GetAttribute("lang", "en");
        /// </code>
        /// </example>
        public string GetAttribute(string key, string defaultValue)
        {
            return Attributes.GetValue(key, defaultValue);
        }
    }
}
