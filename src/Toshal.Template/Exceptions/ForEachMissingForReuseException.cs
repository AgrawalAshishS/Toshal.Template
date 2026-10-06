// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Exceptions
{
    /// <summary>
    /// Thrown by <see cref="Parser.Parse(string)"/> when <c>&lt;%REUSE_FOREACH existing name%&gt;</c> names a FOREACH that is nowhere in the template.
    /// A FOREACH with that id is used first, then the nearest FOREACH with that name above the tag (own level, then outer levels),
    /// then the first one with that name in the whole template.
    /// <see cref="ParserException.Split"/> is null for this exception; the line and column are those of the REUSE_FOREACH tag.
    /// </summary>
    /// <example>
    /// <code>
    /// try { new Parser().Parse("&lt;%REUSE_FOREACH lines copy%&gt;"); }
    /// catch (ForEachMissingForReuseException ex) { Console.WriteLine(ex.ForEachName); }   // lines
    /// </code>
    /// </example>
    public class ForEachMissingForReuseException : ParserException
    {
        /// <summary>
        /// Creates the exception.
        /// </summary>
        /// <param name="reuseName">The new name given in the REUSE_FOREACH tag.</param>
        /// <param name="forEachName">The FOREACH name that was not found.</param>
        /// <param name="lineNumber">The 1 based line of the REUSE_FOREACH tag.</param>
        /// <param name="startingPosition">The 1 based column of the REUSE_FOREACH tag.</param>
        /// <example>
        /// <code>
        /// throw new ForEachMissingForReuseException("copy", "lines", 4, 1);
        /// </code>
        /// </example>
        public ForEachMissingForReuseException(string reuseName, string forEachName, int lineNumber, int startingPosition)
            : base(lineNumber, startingPosition, "FOREACH is missing with name " + forEachName + ", it is referenced in Reuse token named : " + reuseName)
        {
            this.ReuseName = reuseName;
            ForEachName = forEachName;
        }

        /// <summary>
        /// Gets or sets the new name given in the REUSE_FOREACH tag, in lower case.
        /// </summary>
        /// <example>
        /// <code>
        /// string reuse = ex.ReuseName;
        /// </code>
        /// </example>
        public string ReuseName { get; set; }

        /// <summary>
        /// Gets or sets the FOREACH name that was not found, in lower case.
        /// </summary>
        /// <example>
        /// <code>
        /// string missing = ex.ForEachName;
        /// </code>
        /// </example>
        public string ForEachName { get; set; }
    }
}
