// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ForEachToken.cs" company="Toshal Infotech">
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

namespace Toshal.Template.Tokens
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    using Toshal.Template.Exceptions;

    /// <summary>
    ///     The for each token.
    /// </summary>
    public class ForEachToken : Token
    {
        #region Constructors and Destructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="ForEachToken" /> class.
        /// </summary>
        /// <param name="split">
        ///     The split.
        /// </param>
        /// <exception cref="TokenMissingNameException">
        /// </exception>
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

            this.Name = Regex.Match(split.Content, forEachTokenExpression).Groups["Name"].Value.Trim().ToLower();
            if (string.IsNullOrEmpty(this.Name))
            {
                throw new TokenMissingNameException(split);
            }

            this.Name = TokenAttributeDictionary.GetNameAndAttributes(split, Name, this.Attributes);

            this.LineNumber = split.LineNumber;
            this.StartingPosition = split.StartingPosition;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the attributes.
        /// </summary>
        public TokenAttributeDictionary Attributes { get; private set; } = new TokenAttributeDictionary();

        /// <summary>
        ///     Gets the after alt row tokens.
        /// </summary>
        public List<IToken> AfterAltRowTokens { get; private set; }

        /// <summary>
        ///     Gets the after first row tokens.
        /// </summary>
        public List<IToken> AfterFirstRowTokens { get; private set; }

        /// <summary>
        ///     Gets the after last row tokens.
        /// </summary>
        public List<IToken> AfterLastRowTokens { get; private set; }

        /// <summary>
        ///     Gets the after row tokens.
        /// </summary>
        public List<IToken> AfterRowTokens { get; private set; }

        /// <summary>
        ///     Gets the alt row tokens.
        /// </summary>
        public List<IToken> AltRowTokens { get; private set; }

        /// <summary>
        ///     Gets the before alt row tokens.
        /// </summary>
        public List<IToken> BeforeAltRowTokens { get; private set; }

        /// <summary>
        ///     Gets the before first row tokens.
        /// </summary>
        public List<IToken> BeforeFirstRowTokens { get; private set; }

        /// <summary>
        ///     Gets the before last row tokens.
        /// </summary>
        public List<IToken> BeforeLastRowTokens { get; private set; }

        /// <summary>
        ///     Gets the before row tokens.
        /// </summary>
        public List<IToken> BeforeRowTokens { get; private set; }

        /// <summary>
        ///     Gets the first row tokens.
        /// </summary>
        public List<IToken> FirstRowTokens { get; private set; }

        /// <summary>
        ///     Gets the footer tokens.
        /// </summary>
        public List<IToken> FooterTokens { get; private set; }

        /// <summary>
        ///     Gets the header tokens.
        /// </summary>
        public List<IToken> HeaderTokens { get; private set; }

        /// <summary>
        ///     Gets the last row tokens.
        /// </summary>
        public List<IToken> LastRowTokens { get; private set; }

        /// <summary>
        ///     Gets the name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Gets the no record tokens.
        /// </summary>
        public List<IToken> NoRecordTokens { get; private set; }

        /// <summary>
        ///     Gets the row tokens.
        /// </summary>
        public List<IToken> RowTokens { get; private set; }

        #endregion
    }
}