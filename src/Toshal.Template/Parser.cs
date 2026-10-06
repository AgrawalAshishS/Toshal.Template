// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using Toshal.Template.Exceptions;
    using Toshal.Template.Tokens;

    /// <summary>
    /// Turns template text into a list of tokens. Parse a template once and give the tokens to <see cref="Processor.Process(ProcessorArgs)"/>
    /// as often as you like.
    /// </summary>
    /// <remarks>
    /// <para>Tags are written as <c>&lt;%...%&gt;</c>. The keywords (IF, FOREACH, WITH, SET, ...) are upper case and case sensitive.
    /// Names are not case sensitive: the parser lower cases them.</para>
    /// <para>FOREACH names are unique per level. A level is one list of tokens: the top of the template, or the inside of an IF, ELSEIF, ELSE,
    /// WITH, SET or FOREACH part. The same name may be used again at a deeper level, or in the IF part and the ELSE part.
    /// A REUSE_FOREACH uses the nearest FOREACH with its name: first at its own level, then at each outer level up to the top.
    /// When none of those levels has the name, it uses the first FOREACH with that name in the whole template, top to bottom.</para>
    /// <para><b>Warning:</b> a parser keeps state while it works. Parse one template at a time with one instance; do not share an instance between threads.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// List&lt;IToken&gt; tokens = new Parser().Parse("Hello &lt;%=Name%&gt;!");
    /// </code>
    /// </example>
    public class Parser
    {
        #region Constants

        /// <summary>
        ///     The reg ex option.
        /// </summary>
        private const RegexOptions RegExOption = RegexOptions.Singleline;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Parses a template. Plain text becomes <see cref="ContentToken"/>, each tag becomes its token, and blocks such as IF and FOREACH
        /// hold their inner tokens. A REUSE_FOREACH tag is linked to its FOREACH before the method returns.
        /// </summary>
        /// <param name="templateText">The template. An empty string gives an empty list.</param>
        /// <returns>The top level tokens, in template order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="templateText"/> is null.</exception>
        /// <exception cref="TokenMissingNameException">A tag that needs a name has none, for example <c>&lt;%=%&gt;</c>.</exception>
        /// <exception cref="TokenNotClosedException">A block has no end tag, for example IF without ENDIF.</exception>
        /// <exception cref="InvalidTokenAttributeException">The attributes of a tag are not written as <c>name="value"</c>.</exception>
        /// <exception cref="ForEachMissingForReuseException">A REUSE_FOREACH names a FOREACH that is nowhere in the template.</exception>
        /// <exception cref="ParserException">An unknown tag, an end tag without its start, a REMOVE_PREVIOUS count that is missing, not a whole number,
        /// or negative, or two FOREACH blocks with the same name at the same level. All the exceptions above derive from it.</exception>
        /// <example>
        /// <code>
        /// var parser = new Parser();
        /// List&lt;IToken&gt; tokens = parser.Parse("&lt;%IF vip%&gt;Dear &lt;%=Name%&gt;&lt;%ELSE%&gt;Hello&lt;%ENDIF%&gt;");
        /// </code>
        /// </example>
        public List<IToken> Parse(string templateText)
        {
            ArgumentNullException.ThrowIfNull(templateText);

            var retList = new List<IToken>();

            // A parser can be used for many templates. Each template only sees its own FOREACH blocks.
            this._parentLevel.Clear();
            this._forEachByLevel.Clear();
            this._allReuseForEachTokens.Clear();
            this._levels.Clear();

            this._splits = SplitTemplateByTokens(templateText);

            //this._splits =
            //    Regex.Split(templateText, "(?<Name><%.*?%>)", RegExOption)
            //        .Where(x => string.IsNullOrEmpty(x) == false)
            //        .ToArray();
            this._splitIndex = 0;

            if (this.ProcessSplitsTillEnd(retList) == false)
            {
                throw new ParserException(this._splits[this._splitIndex]);
            }

            this.VerifyAllReuseForEachReferences();

            return retList;
        }

        private List<Split> SplitTemplateByTokens(string templateText)
        {
            var retList = new List<Split>();
            int lineNumber = 1;
            int charIndex = 1; // 1 based column of templateText[i]
            var split = new Split { StartingPosition = charIndex, LineNumber = lineNumber };
            var contentBuilder = new StringBuilder();

            for (int i = 0, len = templateText.Length; i < len; i++)
            {
                if (templateText[i] == '<')
                {
                    if (i + 1 < len)
                    {
                        if (templateText[i + 1] == '%')
                        {
                            if (contentBuilder.Length > 0)
                            {
                                split.Content = contentBuilder.ToString();
                                retList.Add(split);
                                contentBuilder.Clear();
                            }

                            split = new Split();
                            split.StartingPosition = charIndex;
                            split.LineNumber = lineNumber;
                            contentBuilder.Append("<%");
                            i++;
                            charIndex++;
                            charIndex++;
                            continue;
                        }
                    }
                }
                else if (templateText[i] == '%')
                {
                    if (i + 1 < len)
                    {
                        if (templateText[i + 1] == '>')
                        {
                            if (contentBuilder.Length == 0)
                            {
                                // "%>" with no open tag and no text before it is plain text.
                                contentBuilder.Append("%>");
                                i++;
                                charIndex++;
                                charIndex++;
                                continue;
                            }

                            if (contentBuilder.Length > 0)
                            {
                                split.Content = contentBuilder.ToString();
                                retList.Add(split);
                                contentBuilder.Clear();
                            }
                            split.Content += "%>" ;
                            split = new Split();
                            split.StartingPosition = charIndex + 2; // the text after "%>"
                            split.LineNumber = lineNumber;
                            i++;
                            charIndex++;
                            charIndex++;
                            continue;
                        }
                    }
                }

                contentBuilder.Append(templateText[i]);
                charIndex++;
                if (templateText[i] == '\n')
                {
                    lineNumber++;
                    charIndex = 1;
                }
            }

            if (contentBuilder.Length > 0)
            {
                split.Content = contentBuilder.ToString();
                retList.Add(split);
            }

            return retList;
        }

        #endregion

        #region Fields

        // A level is one token list: the top of the template, or the inside of an IF, ELSEIF, ELSE, WITH, SET or FOREACH part.
        // Each level points to the level around it (null for the top), so REUSE_FOREACH can look from its own level outwards.
        private readonly Dictionary<List<IToken>, List<IToken>?> _parentLevel = new Dictionary<List<IToken>, List<IToken>?>(ReferenceEqualityComparer.Instance);

        // The FOREACH blocks of each level by name. A name may be used once per level, and again at a deeper level.
        private readonly Dictionary<List<IToken>, Dictionary<string, ForEachToken>> _forEachByLevel = new Dictionary<List<IToken>, Dictionary<string, ForEachToken>>(ReferenceEqualityComparer.Instance);

        // Every REUSE_FOREACH with the level it is in. They are linked after the whole template is parsed,
        // so a REUSE_FOREACH may come before its FOREACH.
        private readonly List<(ReuseForEachToken Token, List<IToken> Level)> _allReuseForEachTokens = new List<(ReuseForEachToken Token, List<IToken> Level)>();

        // The levels being filled right now, the innermost on top.
        private readonly Stack<List<IToken>> _levels = new Stack<List<IToken>>();

        /// <summary>
        ///     The _split index.
        /// </summary>
        private int _splitIndex;

        /// <summary>
        ///     The _splits.
        /// </summary>
        private List<Split> _splits = new List<Split>();

        #endregion

        #region Methods

        /// <summary>
        ///     The check for inner block.
        /// </summary>
        /// <param name="split">
        ///     The split.
        /// </param>
        /// <param name="name">
        ///     The name.
        /// </param>
        /// <param name="containerTag">
        ///     The container tag.
        /// </param>
        /// <param name="tokenList">
        ///     The token list.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        /// <exception cref="TokenNotClosedException">
        /// </exception>
        private bool CheckForInnerBlock(Split split, string name, string containerTag, List<IToken> tokenList)
        {
            if (this._splits[this._splitIndex].Content == "<%" + containerTag + "%>")
            {
                var tempSplit = this._splits[this._splitIndex];

                this._splitIndex++;
                if (this.ProcessSplitsTillEnd(tokenList))
                {
                    throw new TokenNotClosedException(tempSplit, name, "<%" + containerTag + "%> not closed for " + split.Content);
                }

                if (this._splits[this._splitIndex].Content != "<%END" + containerTag + "%>")
                {
                    throw new TokenNotClosedException(tempSplit, name, "<%" + containerTag + "%> not closed for " + split.Content);
                }

                this._splitIndex++;

                return true;
            }

            return false;
        }

        /// <summary>
        ///     The create condition element.
        /// </summary>
        /// <param name="split">
        ///     The split.
        /// </param>
        /// <returns>
        ///     The <see cref="ConditionToken" />.
        /// </returns>
        /// <exception cref="TokenNotClosedException">
        /// </exception>
        private ConditionToken CreateConditionElement(Split split)
        {
            var token = new ConditionToken(split);

            this._splitIndex++;
            if (this.ProcessSplitsTillEnd(token.InnerTokens))
            {
                throw new TokenNotClosedException(split, token.Name);
            }

            if (this._splits[this._splitIndex].Content.StartsWith("<%ELSEIF "))
            {
                token.FalsePart = this.CreateConditionElement(this._splits[this._splitIndex]);
            }
            else
            {
                if (this._splits[this._splitIndex].Content == "<%ELSE%>")
                {
                    token.FalsePart = new ElseToken(this._splits[this._splitIndex]);
                    this._splitIndex++;
                    if (this.ProcessSplitsTillEnd(((ElseToken)token.FalsePart).InnerTokens))
                    {
                        throw new TokenNotClosedException(split, token.Name);
                    }
                }

                if (this._splits[this._splitIndex].Content != "<%ENDIF%>")
                {
                    throw new TokenNotClosedException(split, token.Name);
                }
            }

            return token;
        }

        /// <summary>
        ///     The handled as condition token.
        /// </summary>
        /// <param name="split">
        ///     The split.
        /// </param>
        /// <param name="tokenList">
        ///     The token list.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        private bool HandledAsConditionToken(Split split, List<IToken> tokenList)
        {
            if (split.Content.StartsWith("<%IF ") == false)
            {
                return false;
            }

            var token = this.CreateConditionElement(split);
            tokenList.Add(token);
            return true;
        }

        /// <summary>
        ///     The handled as content token.
        /// </summary>
        /// <param name="split">
        ///     The split.
        /// </param>
        /// <param name="tokenList">
        ///     The token list.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        private bool HandledAsContentToken(Split split, List<IToken> tokenList)
        {
            if (split.Content.StartsWith("<%"))
            {
                return false;
            }

            tokenList.Add(new ContentToken(split));
            return true;
        }

        /// <summary>
        ///     The handled as for each.
        /// </summary>
        /// <param name="split">
        ///     The split.
        /// </param>
        /// <param name="tokenList">
        ///     The token list.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        /// <exception cref="TokenNotClosedException">
        /// </exception>
        /// <exception cref="ParserException">
        /// </exception>
        private bool HandledAsForEach(Split split, List<IToken> tokenList)
        {
            if (split.Content.StartsWith("<%FOREACH ") == false)
            {
                return false;
            }

            var token = new ForEachToken(split);

            this._splitIndex++;

            var tokenTypes = new Dictionary<string, List<IToken>>
                                 {
                                     { "NORECORD", token.NoRecordTokens },
                                     { "HEADER", token.HeaderTokens },
                                     { "BEFOREFIRSTROW", token.BeforeFirstRowTokens },
                                     { "FIRSTROW", token.FirstRowTokens },
                                     { "AFTERFIRSTROW", token.AfterFirstRowTokens },
                                     { "BEFOREROW", token.BeforeRowTokens },
                                     { "ROW", token.RowTokens },
                                     { "AFTERROW", token.AfterRowTokens },
                                     { "BEFOREALTROW", token.BeforeAltRowTokens },
                                     { "ALTROW", token.AltRowTokens },
                                     { "AFTERALTROW", token.AfterAltRowTokens },
                                     { "BEFORELASTROW", token.BeforeLastRowTokens },
                                     { "LASTROW", token.LastRowTokens },
                                     { "AFTERLASTROW", token.AfterLastRowTokens },
                                     { "FOOTER", token.FooterTokens }
                                 };

            while (true)
            {
                this.ProcessSplitsTillEnd(token.RowTokens);
                if (this._splitIndex == this._splits.Count)
                {
                    throw new TokenNotClosedException(split, token.Name);
                }

                if (tokenTypes.Keys.Count == 0)
                {
                    // all known tokens are parsed - this is unknown token
                    throw new ParserException(this._splits[this._splitIndex]);
                }

                var beforeCount = tokenTypes.Keys.Count;

                foreach (var tag in tokenTypes.Keys)
                {
                    if (this.CheckForInnerBlock(split, token.Name, tag, tokenTypes[tag]))
                    {
                        tokenTypes.Remove(tag);
                        break;
                    }
                }

                if (this._splitIndex == this._splits.Count)
                {
                    throw new TokenNotClosedException(split, token.Name);
                }

                if (this._splits[this._splitIndex].Content == "<%ENDFOR%>")
                {
                    break;
                }

                if (beforeCount == tokenTypes.Keys.Count)
                {
                    // none of inner token is matching - this is unknown token
                    throw new ParserException(this._splits[this._splitIndex]);
                }
            }

            // A FOREACH name is unique per level. The second one is reported at its own position.
            if (this._forEachByLevel.TryGetValue(tokenList, out var forEachByName) == false)
            {
                forEachByName = new Dictionary<string, ForEachToken>();
                this._forEachByLevel[tokenList] = forEachByName;
            }

            if (forEachByName.TryAdd(token.Name, token) == false)
            {
                throw new ParserException(split, "FOREACH name " + token.Name + " is used twice at the same level. Rename one, or move it into a block.");
            }

            tokenList.Add(token);
            return true;
        }

        /// <summary>
        ///     The handled as named token.
        /// </summary>
        /// <param name="split">
        ///     The split.
        /// </param>
        /// <param name="tokenList">
        ///     The token list.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        private bool HandledAsNamedToken(Split split, List<IToken> tokenList)
        {
            if (split.Content.StartsWith("<%=") == false)
            {
                return false;
            }

            tokenList.Add(new NamedToken(split));
            return true;
        }

        /// <summary>
        ///     The handled as reuse for each.
        /// </summary>
        /// <param name="split">
        ///     The split.
        /// </param>
        /// <param name="tokenList">
        ///     The token list.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        private bool HandledAsReuseForEach(Split split, List<IToken> tokenList)
        {
            if (split.Content.StartsWith("<%REUSE_FOREACH ") == false)
            {
                return false;
            }

            var token = new ReuseForEachToken(split);
            tokenList.Add(token);
            this._allReuseForEachTokens.Add((token, tokenList));
            return true;
        }

        /// <summary>
        ///     The handled as with.
        /// </summary>
        /// <param name="split">
        ///     The split.
        /// </param>
        /// <param name="tokenList">
        ///     The token list.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        /// <exception cref="TokenNotClosedException">
        /// </exception>
        private bool HandledAsWith(Split split, List<IToken> tokenList)
        {
            if (split.Content.StartsWith("<%WITH ") == false)
            {
                return false;
            }

            var token = new WithToken(split);

            this._splitIndex++;
            if (this.ProcessSplitsTillEnd(token.InnerTokens))
            {
                throw new TokenNotClosedException(split, token.Name);
            }

            var lastSplit = this._splits[this._splitIndex];
            if (lastSplit.Content != "<%ENDWITH%>")
            {
                throw new TokenNotClosedException(split, token.Name);
            }

            tokenList.Add(token);
            return true;
        }

        /// <summary>
        ///     The handled as SET tokens.
        /// </summary>
        /// <param name="split">
        ///     The split.
        /// </param>
        /// <param name="tokenList">
        ///     The token list.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        private bool HandledAsSet(Split split, List<IToken> tokenList)
        {
            if (split.Content.StartsWith("<%SET ") == false)
            {
                return false;
            }

            var token = new SetToken(split);

            this._splitIndex++;
            if (this.ProcessSplitsTillEnd(token.InnerTokens))
            {
                throw new TokenNotClosedException(split, token.Name);
            }

            var lastSplit = this._splits[this._splitIndex];
            if (lastSplit.Content != "<%ENDSET%>")
            {
                throw new TokenNotClosedException(split, token.Name);
            }

            tokenList.Add(token);
            return true;
        }

        private bool HandledAsRemovePreviousNewLine(Split split, List<IToken> tokenList)
        {
            if (split.Content != "<%REMOVE_PREVIOUS_NEW_LINE%>")
            {
                return false;
            }

            tokenList.Add(new RemovePreviousNewLineToken(split));

            return true;
        }

        private bool HandledAsRemovePreviousChars(Split split, List<IToken> tokenList)
        {
            if (split.Content.StartsWith("<%REMOVE_PREVIOUS ") == false)
            {
                return false;
            }

            tokenList.Add(new RemovePreviousCharsToken(split));

            return true;
        }

        private bool HandledAsProcessTemplate(Split split, List<IToken> tokenList)
        {
            if (split.Content.StartsWith("<%PROCESS_TEMPLATE ") == false)
            {
                return false;
            }

            tokenList.Add(new ProcessTemplateToken(split));

            return true;
        }

        private bool HandledAsContextAsString(Split split, List<IToken> tokenList)
        {
            if (split.Content != "<%CONTEXT_AS_STRING%>")
            {
                return false;
            }

            tokenList.Add(new ContextAsStringToken(split));

            return true;
        }

        /// <summary>
        ///     The process splits till end.
        /// </summary>
        /// <param name="tokenList">
        ///     The token list.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        private bool ProcessSplitsTillEnd(List<IToken> tokenList)
        {
            // The first time a list is filled, the list being filled around it is its outer level.
            if (this._parentLevel.ContainsKey(tokenList) == false)
            {
                this._parentLevel[tokenList] = this._levels.Count > 0 ? this._levels.Peek() : null;
            }

            this._levels.Push(tokenList);
            try
            {
                return this.ProcessSplitsTillEndOfLevel(tokenList);
            }
            finally
            {
                this._levels.Pop();
            }
        }

        private bool ProcessSplitsTillEndOfLevel(List<IToken> tokenList)
        {
            for (; this._splitIndex < this._splits.Count; this._splitIndex++)
            {
                var split = this._splits[this._splitIndex];

                if (this.HandledAsContentToken(split, tokenList))
                {
                    continue;
                }

                if (this.HandledAsNamedToken(split, tokenList))
                {
                    continue;
                }

                if (this.HandledAsConditionToken(split, tokenList))
                {
                    continue;
                }

                if (this.HandledAsForEach(split, tokenList))
                {
                    continue;
                }

                if (this.HandledAsWith(split, tokenList))
                {
                    continue;
                }

                if (this.HandledAsReuseForEach(split, tokenList))
                {
                    continue;
                }

                if (this.HandledAsSet(split, tokenList))
                {
                    continue;
                }

                if (this.HandledAsRemovePreviousNewLine(split, tokenList))
                {
                    continue;
                }

                if (this.HandledAsRemovePreviousChars(split, tokenList))
                {
                    continue;
                }

                if (this.HandledAsProcessTemplate(split, tokenList))
                {
                    continue;
                }

                if (this.HandledAsContextAsString(split, tokenList))
                {
                    continue;
                }

                return false; // not reaching to end. Meaning some closing tag.
            }

            return true; // reached to end
        }

        /// <summary>
        ///     The verify all reuse for each references.
        /// </summary>
        /// <exception cref="ForEachMissingForReuseException">
        /// </exception>
        private void VerifyAllReuseForEachReferences()
        {
            if (this._allReuseForEachTokens.Count == 0)
            {
                return;
            }

            foreach (var (reuseForEachToken, level) in this._allReuseForEachTokens)
            {
                // The nearest FOREACH wins: the level of the REUSE_FOREACH first, then each outer level up to the top.
                ForEachToken? found = null;
                for (List<IToken>? current = level; current != null && found == null; current = this._parentLevel[current])
                {
                    if (this._forEachByLevel.TryGetValue(current, out var forEachByName))
                    {
                        forEachByName.TryGetValue(reuseForEachToken.ExistingForEachName, out found);
                    }
                }

                // Fallback: the first FOREACH with the name in the whole template, top to bottom. Blocks are finished inner first
                // while parsing, so template order comes from the line and column, not from the order the blocks were found.
                found ??= this._forEachByLevel.Values
                    .Select(byName => byName.GetValueOrDefault(reuseForEachToken.ExistingForEachName))
                    .OfType<ForEachToken>()
                    .OrderBy(t => t.LineNumber)
                    .ThenBy(t => t.StartingPosition)
                    .FirstOrDefault();

                reuseForEachToken.ExistingForEachToken = found
                    ?? throw new ForEachMissingForReuseException(reuseForEachToken.Name, reuseForEachToken.ExistingForEachName, reuseForEachToken.LineNumber, reuseForEachToken.StartingPosition);
            }
        }

        #endregion
    }
}