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
        /// <param name="templateText">The template. Must not be null. An empty string gives an empty list.</param>
        /// <returns>The top level tokens, in template order.</returns>
        /// <exception cref="TokenMissingNameException">A tag that needs a name has none, for example <c>&lt;%=%&gt;</c>.</exception>
        /// <exception cref="TokenNotClosedException">A block has no end tag, for example IF without ENDIF.</exception>
        /// <exception cref="InvalidTokenAttributeException">The attributes of a tag are not written as <c>name="value"</c>.</exception>
        /// <exception cref="ForEachMissingForReuseException">A REUSE_FOREACH names a FOREACH that is not in the template.</exception>
        /// <exception cref="ParserException">An unknown tag, an end tag without its start, or a bad REMOVE_PREVIOUS count.
        /// All the exceptions above derive from it.</exception>
        /// <exception cref="ArgumentException">Known issue: REUSE_FOREACH names a FOREACH name that is used more than once, or a tag repeats an attribute.
        /// See docs/known-issues.md.</exception>
        /// <example>
        /// <code>
        /// var parser = new Parser();
        /// List&lt;IToken&gt; tokens = parser.Parse("&lt;%IF vip%&gt;Dear &lt;%=Name%&gt;&lt;%ELSE%&gt;Hello&lt;%ENDIF%&gt;");
        /// </code>
        /// </example>
        public List<IToken> Parse(string templateText)
        {
            var retList = new List<IToken>();

            // A parser can be used for many templates. Each template only sees its own FOREACH blocks.
            this._allForEachTokens.Clear();
            this._allReuseForEachTokens.Clear();

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

        /// <summary>
        ///     The _all for each tokens.
        /// </summary>
        private readonly List<ForEachToken> _allForEachTokens = new List<ForEachToken>();

        /// <summary>
        ///     The _all reuse for each tokens.
        /// </summary>
        private readonly List<ReuseForEachToken> _allReuseForEachTokens = new List<ReuseForEachToken>();

        /// <summary>
        ///     The _split index.
        /// </summary>
        private int _splitIndex;

        /// <summary>
        ///     The _splits.
        /// </summary>
        private List<Split> _splits = null!;

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

            tokenList.Add(token);
            this._allForEachTokens.Add(token);
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
            this._allReuseForEachTokens.Add(token);
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

            foreach (var reuseForEachToken in this._allReuseForEachTokens)
            {
                var matches = this._allForEachTokens.Where(t => t.Name == reuseForEachToken.ExistingForEachName).ToList();
                if (matches.Count > 1)
                {
                    throw new ArgumentException("FOREACH name " + reuseForEachToken.ExistingForEachName + " is used more than once, so REUSE_FOREACH " + reuseForEachToken.Name + " cannot pick one.");
                }

                if (matches.Count == 1)
                {
                    reuseForEachToken.ExistingForEachToken = matches[0];
                }
                else
                {
                    throw new ForEachMissingForReuseException(reuseForEachToken.Name, reuseForEachToken.ExistingForEachName, reuseForEachToken.LineNumber, reuseForEachToken.StartingPosition);
                }
            }
        }

        #endregion
    }
}