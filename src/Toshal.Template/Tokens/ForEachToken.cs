// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Tokens
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using Toshal.Template.Exceptions;

    /// <summary>
    /// A parsed <c>&lt;%FOREACH name%&gt;</c> ... <c>&lt;%ENDFOR%&gt;</c> block. It has one token list for each part that a loop can have.
    /// Each part is written as <c>&lt;%PART%&gt;</c> ... <c>&lt;%ENDPART%&gt;</c> inside the block, for example <c>&lt;%HEADER%&gt;</c> ... <c>&lt;%ENDHEADER%&gt;</c>,
    /// and each part may appear once. Text and tags inside the block but outside every part belong to the ROW part.
    /// </summary>
    /// <remarks>
    /// <para>For each row the processor picks the parts like this: on odd rows (the second, fourth, ...) the ALT parts are used when they are not empty.
    /// On the first row of a list with more than one row, the FIRST parts win. On the last row, the LAST parts win.
    /// An empty part is ignored and the earlier choice stays, so an odd last row with no LASTROW part uses ALTROW, or ROW when ALTROW is empty too.</para>
    /// <para><b>Warning:</b> by design the LAST parts win: a list with one row uses BEFORELASTROW, LASTROW and AFTERLASTROW, and never the FIRST parts.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// &lt;%FOREACH lines%&gt;
    ///   &lt;%HEADER%&gt;Item  Qty&lt;%ENDHEADER%&gt;
    ///   &lt;%ROW%&gt;&lt;%=Item%&gt;  &lt;%=Qty%&gt;&lt;%ENDROW%&gt;
    ///   &lt;%NORECORD%&gt;No lines&lt;%ENDNORECORD%&gt;
    /// &lt;%ENDFOR%&gt;
    /// </code>
    /// </example>
    public class ForEachToken : Token
    {
        /// <summary>
        /// Reads the name and attributes of a FOREACH tag and creates empty part lists. The parser calls it and then fills the parts.
        /// </summary>
        /// <param name="split">The FOREACH tag.</param>
        /// <exception cref="TokenMissingNameException">The tag has no name, for example <c>&lt;%FOREACH %&gt;</c>.</exception>
        /// <exception cref="InvalidTokenAttributeException">The attributes are not written as <c>name="value"</c>.</exception>
        /// <example>
        /// <code>
        /// var token = new ForEachToken(new Split { Content = "&lt;%FOREACH lines%&gt;" });
        /// </code>
        /// </example>
        public ForEachToken(Split split)
        {
            this.NoRecordTokens = new List<IToken>();

            this.HeaderTokens = new List<IToken>();

            this.BeforeFirstRowTokens = new List<IToken>();
            this.FirstRowTokens = new List<IToken>();
            this.AfterFirstRowTokens = new List<IToken>();

            this.BeforeRowTokens = new List<IToken>();
            this.RowTokens = new List<IToken>();
            this.AfterRowTokens = new List<IToken>();

            this.BeforeAltRowTokens = new List<IToken>();
            this.AltRowTokens = new List<IToken>();
            this.AfterAltRowTokens = new List<IToken>();

            this.BeforeLastRowTokens = new List<IToken>();
            this.LastRowTokens = new List<IToken>();
            this.AfterLastRowTokens = new List<IToken>();

            this.FooterTokens = new List<IToken>();

            const string forEachTokenExpression = "<%FOREACH\\s(?<Name>.*?)%>";

            this.Name = Regex.Match(split.Content, forEachTokenExpression).Groups["Name"].Value.Trim();
            if (string.IsNullOrEmpty(this.Name))
            {
                throw new TokenMissingNameException(split);
            }

            this.Name = TokenAttributeDictionary.GetNameAndAttributes(split, Name, this.Attributes);

            this.LineNumber = split.LineNumber;
            this.StartingPosition = split.StartingPosition;
        }

        /// <summary>
        /// Gets the attributes of the FOREACH tag. Keys are lower case.
        /// </summary>
        /// <remarks>
        /// <para>Attribute names are lower case; values keep the case the template author wrote. For a lower case copy of the values use <see cref="TokenAttributeDictionary.LowerCaseValues"/>.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// string top = token.Attributes.GetValue("top", "10");
        /// </code>
        /// </example>
        public TokenAttributeDictionary Attributes { get; private set; } = new TokenAttributeDictionary();

        /// <summary>Gets the AFTERALTROW part: written after each odd row (second, fourth, ...) instead of AFTERROW, when not empty.</summary>
        /// <example><code>int count = token.AfterAltRowTokens.Count;</code></example>
        public List<IToken> AfterAltRowTokens { get; private set; }

        /// <summary>Gets the AFTERFIRSTROW part: written after the first row instead of AFTERROW, when not empty and the list has more than one row.</summary>
        /// <example><code>int count = token.AfterFirstRowTokens.Count;</code></example>
        public List<IToken> AfterFirstRowTokens { get; private set; }

        /// <summary>Gets the AFTERLASTROW part: written after the last row instead of AFTERROW, when not empty.</summary>
        /// <example><code>int count = token.AfterLastRowTokens.Count;</code></example>
        public List<IToken> AfterLastRowTokens { get; private set; }

        /// <summary>Gets the AFTERROW part: written after each row. Its context is the row item.</summary>
        /// <example><code>int count = token.AfterRowTokens.Count;</code></example>
        public List<IToken> AfterRowTokens { get; private set; }

        /// <summary>Gets the ALTROW part: written for each odd row (second, fourth, ...) instead of ROW, when not empty.</summary>
        /// <example><code>int count = token.AltRowTokens.Count;</code></example>
        public List<IToken> AltRowTokens { get; private set; }

        /// <summary>Gets the BEFOREALTROW part: written before each odd row (second, fourth, ...) instead of BEFOREROW, when not empty.</summary>
        /// <example><code>int count = token.BeforeAltRowTokens.Count;</code></example>
        public List<IToken> BeforeAltRowTokens { get; private set; }

        /// <summary>Gets the BEFOREFIRSTROW part: written before the first row instead of BEFOREROW, when not empty and the list has more than one row.</summary>
        /// <example><code>int count = token.BeforeFirstRowTokens.Count;</code></example>
        public List<IToken> BeforeFirstRowTokens { get; private set; }

        /// <summary>Gets the BEFORELASTROW part: written before the last row instead of BEFOREROW, when not empty.</summary>
        /// <example><code>int count = token.BeforeLastRowTokens.Count;</code></example>
        public List<IToken> BeforeLastRowTokens { get; private set; }

        /// <summary>Gets the BEFOREROW part: written before each row. Its context is the row item.</summary>
        /// <example><code>int count = token.BeforeRowTokens.Count;</code></example>
        public List<IToken> BeforeRowTokens { get; private set; }

        /// <summary>Gets the FIRSTROW part: written for the first row instead of ROW, when not empty and the list has more than one row.</summary>
        /// <example><code>int count = token.FirstRowTokens.Count;</code></example>
        public List<IToken> FirstRowTokens { get; private set; }

        /// <summary>Gets the FOOTER part: written once after all rows when the list has rows. Its context is the list.</summary>
        /// <example><code>int count = token.FooterTokens.Count;</code></example>
        public List<IToken> FooterTokens { get; private set; }

        /// <summary>Gets the HEADER part: written once before all rows when the list has rows. Its context is the list.</summary>
        /// <example><code>int count = token.HeaderTokens.Count;</code></example>
        public List<IToken> HeaderTokens { get; private set; }

        /// <summary>Gets the LASTROW part: written for the last row instead of ROW, when not empty.</summary>
        /// <example><code>int count = token.LastRowTokens.Count;</code></example>
        public List<IToken> LastRowTokens { get; private set; }

        /// <summary>
        /// Gets the loop name in lower case, without the attributes.
        /// </summary>
        /// <example>
        /// <code>
        /// string name = token.Name;   // "lines" for &lt;%FOREACH Lines%&gt;
        /// </code>
        /// </example>
        public string Name { get; }

        /// <summary>Gets the NORECORD part: written instead of everything else when the loop value provider returns null or an empty list. Its context is the context around the loop.</summary>
        /// <example><code>int count = token.NoRecordTokens.Count;</code></example>
        public List<IToken> NoRecordTokens { get; private set; }

        /// <summary>Gets the ROW part: written for each row. It also holds everything inside the block that is outside every other part.</summary>
        /// <example><code>int count = token.RowTokens.Count;</code></example>
        public List<IToken> RowTokens { get; private set; }
    }
}
