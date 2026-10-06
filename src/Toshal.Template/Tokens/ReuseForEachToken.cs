// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Tokens
{

    using Toshal.Template.Exceptions;

    /// <summary>
    /// A parsed <c>&lt;%REUSE_FOREACH existing name%&gt;</c> tag. It runs the FOREACH block named <c>existing</c> again, at this place, under the new name.
    /// The loop value provider is asked for the rows with the new name and the attributes of the existing FOREACH,
    /// so the same layout can show a different list. The parser links the FOREACH whose <c>id</c> attribute is <c>existing</c>, wherever it is.
    /// Without such an id it links the nearest FOREACH named <c>existing</c> above this tag: at the level of this tag first, then at each outer level.
    /// When none is found, it links the first FOREACH with that name in the whole template, top to bottom.
    /// </summary>
    /// <example>
    /// <code>
    /// // Same table layout for two lists:
    /// // &lt;%FOREACH open%&gt;- &lt;%=Title%&gt;&lt;%ENDFOR%&gt;  Done: &lt;%REUSE_FOREACH open done%&gt;
    /// // Pick one of two layouts of the same list by its id:
    /// // &lt;%FOREACH open id="short"%&gt;&lt;%=Title%&gt;&lt;%ENDFOR%&gt;&lt;%FOREACH open%&gt;...&lt;%ENDFOR%&gt;  &lt;%REUSE_FOREACH short done%&gt;
    /// </code>
    /// </example>
    public sealed class ReuseForEachToken : Token
    {
        /// <summary>
        /// Reads the existing FOREACH name and the new name from the tag. The parser calls it and later sets <see cref="ExistingForEachToken"/>.
        /// </summary>
        /// <param name="split">The tag.</param>
        /// <exception cref="TokenMissingNameException">The tag does not have both names.</exception>
        /// <example>
        /// <code>
        /// var token = new ReuseForEachToken(new Split { Content = "&lt;%REUSE_FOREACH open done%&gt;" });
        /// </code>
        /// </example>
        public ReuseForEachToken(Split split)
        {
            this.Name = string.Empty;

            var (existing, name) = TagText.ReuseNames(split.Content);

            this.ExistingForEachName = existing.Trim().ToLower();
            this.Name = name.Trim().ToLower();
            this.LineNumber = split.LineNumber;
            this.StartingPosition = split.StartingPosition;

            if (string.IsNullOrEmpty(this.Name))
            {
                throw new TokenMissingNameException(split);
            }
        }

        /// <summary>
        /// Gets the id or the name of the FOREACH to reuse, in lower case. An id wins over a name.
        /// </summary>
        /// <example>
        /// <code>
        /// string existing = token.ExistingForEachName;   // "open"
        /// </code>
        /// </example>
        public string ExistingForEachName { get; private set; }

        /// <summary>
        /// Gets or sets the FOREACH block to reuse. <see cref="Parser.Parse(string)"/> sets it before it returns; it is null only on a token made by hand.
        /// </summary>
        /// <example>
        /// <code>
        /// ForEachToken layout = token.ExistingForEachToken!;
        /// </code>
        /// </example>
        public ForEachToken? ExistingForEachToken { get; set; }

        /// <summary>
        /// Gets the new name, in lower case. The loop value provider gets it as <see cref="ArgsBase.Name"/>.
        /// </summary>
        /// <example>
        /// <code>
        /// string name = token.Name;   // "done"
        /// </code>
        /// </example>
        public string Name { get; }
    }
}
