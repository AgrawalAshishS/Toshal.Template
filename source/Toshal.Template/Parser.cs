// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Parser.cs" company="Toshal Infotech">
//   http://www.ToshalInfotech.com
//   Copyright (c) 2014-2015
//   by Toshal Infotech
//   
//   Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
//   documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
//   the rights to use, copy, modify, merge, publish, distribute, sub-license, and/or sell copies of the Software, and 
//   to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//   
//   The above copyright notice and this permission notice shall be included in all copies or substantial portions 
//   of the Software.
//   
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
//   TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
//   THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
//   CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
//   DEALINGS IN THE SOFTWARE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Toshal.Template
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    using Toshal.Template.Exceptions;
    using Toshal.Template.Tokens;

    /// <summary>
    ///     The parser.
    /// </summary>
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
        ///     The parse.
        /// </summary>
        /// <param name="templateText">
        ///     The template text.
        /// </param>
        /// <returns>
        ///     The <see cref="List" />.
        /// </returns>
        /// <exception cref="ParserException">
        /// </exception>
        public List<IToken> Parse(string templateText)
        {
            var retList = new List<IToken>();
            this._splits =
                Regex.Split(templateText, "(?<Name><%.*?%>)", RegExOption)
                    .Where(x => string.IsNullOrEmpty(x) == false)
                    .ToArray();
            this._splitIndex = 0;

            if (this.ProcessSplitsTillEnd(retList) == false)
            {
                throw new ParserException(this._splits[this._splitIndex]);
            }

            this.VerifyAllReuseForEachReferences();

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
        private string[] _splits;

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
        private bool CheckForInnerBlock(string split, string name, string containerTag, List<IToken> tokenList)
        {
            if (this._splits[this._splitIndex] == "<%" + containerTag + "%>")
            {
                this._splitIndex++;
                if (this.ProcessSplitsTillEnd(tokenList))
                {
                    throw new TokenNotClosedException(split, name, "<%" + containerTag + "%> not closed for " + split);
                }

                if (this._splits[this._splitIndex] != "<%END" + containerTag + "%>")
                {
                    throw new TokenNotClosedException(split, name, "<%" + containerTag + "%> not closed for " + split);
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
        private ConditionToken CreateConditionElement(string split)
        {
            var token = new ConditionToken(split);

            this._splitIndex++;
            if (this.ProcessSplitsTillEnd(token.InnerTokens))
            {
                throw new TokenNotClosedException(split, token.Name);
            }

            if (this._splits[this._splitIndex].StartsWith("<%ELSEIF "))
            {
                token.FalsePart = this.CreateConditionElement(this._splits[this._splitIndex]);
            }
            else
            {
                if (this._splits[this._splitIndex] == "<%ELSE%>")
                {
                    this._splitIndex++;
                    token.FalsePart = new ElseToken();
                    if (this.ProcessSplitsTillEnd(((ElseToken)token.FalsePart).InnerTokens))
                    {
                        throw new TokenNotClosedException(split, token.Name);
                    }
                }

                if (this._splits[this._splitIndex] != "<%ENDIF%>")
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
        private bool HandledAsConditionToken(string split, List<IToken> tokenList)
        {
            if (split.StartsWith("<%IF ") == false)
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
        private bool HandledAsContentToken(string split, List<IToken> tokenList)
        {
            if (split.StartsWith("<%"))
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
        private bool HandledAsForEach(string split, List<IToken> tokenList)
        {
            if (split.StartsWith("<%FOREACH ") == false)
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
                if (this._splitIndex == this._splits.Length)
                {
                    throw new TokenNotClosedException(split, token.Name);
                }

                if (tokenTypes.Keys.Count == 0)
                {
                    // all known tokens are parsed - this is unknown token
                    throw new ParserException(this._splits[this._splitIndex], "Unknown token.");
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

                if (this._splitIndex == this._splits.Length)
                {
                    throw new TokenNotClosedException(split, token.Name);
                }

                if (this._splits[this._splitIndex] == "<%ENDFOR%>")
                {
                    break;
                }

                if (beforeCount == tokenTypes.Keys.Count)
                {
                    // none of inner token is matching - this is unknown token
                    throw new ParserException(this._splits[this._splitIndex], "Unknown token.");
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
        private bool HandledAsNamedToken(string split, List<IToken> tokenList)
        {
            if (split.StartsWith("<%=") == false)
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
        private bool HandledAsReuseForEach(string split, List<IToken> tokenList)
        {
            if (split.StartsWith("<%REUSEFOREACH ") == false)
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
        private bool HandledAsWith(string split, List<IToken> tokenList)
        {
            if (split.StartsWith("<%WITH ") == false)
            {
                return false;
            }

            var token = new WithToken(split);

            this._splitIndex++;
            if (this.ProcessSplitsTillEnd(token.InnerTokens))
            {
                throw new TokenNotClosedException(split, token.Name);
            }

            tokenList.Add(token);
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
            for (; this._splitIndex < this._splits.Length; this._splitIndex++)
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
                foreach (var forEachToken in this._allForEachTokens)
                {
                    if (reuseForEachToken.ExistingForEachName == forEachToken.Name)
                    {
                        reuseForEachToken.ExistingForEachToken = forEachToken;
                        break;
                    }
                }

                if (reuseForEachToken.ExistingForEachToken == null)
                {
                    throw new ForEachMissingForReuseException(reuseForEachToken.Name);
                }
            }
        }

        #endregion
    }
}