// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ReuseForEachToken.cs" company="Toshal Infotech">
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
    using System.Text.RegularExpressions;

    using Toshal.Template.Exceptions;

    /// <summary>
    ///     The reuse for each token.
    /// </summary>
    public class ReuseForEachToken : Token
    {
        #region Constructors and Destructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="ReuseForEachToken" /> class.
        /// </summary>
        /// <param name="split">
        ///     The split.
        /// </param>
        /// <exception cref="TokenMissingNameException">
        /// </exception>
        public ReuseForEachToken(Split split)
        {
            this.Name = string.Empty;

            const string reuseForEachTokenExpression = "<%REUSEFOREACH\\s(?<ExistingForEachName>.+?)\\s(?<Name>.+?)%>";
            var m = Regex.Match(split.Content, reuseForEachTokenExpression);

            this.ExistingForEachName = m.Groups["ExistingForEachName"].Value.Trim().ToLower();
            this.Name = m.Groups["Name"].Value.Trim().ToLower();

            this.LineNumber = split.LineNumber;
            this.StartingPosition = split.StartingPosition;

            if (string.IsNullOrEmpty(this.Name))
            {
                throw new TokenMissingNameException(split);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the existing for each name.
        /// </summary>
        public string ExistingForEachName { get; private set; }

        /// <summary>
        ///     Gets or sets the existing for each token.
        /// </summary>
        public ForEachToken ExistingForEachToken { get; set; }

        /// <summary>
        ///     Gets the name.
        /// </summary>
        public string Name { get; }

        #endregion
    }
}