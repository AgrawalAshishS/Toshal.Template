// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NamedToken.cs" company="Toshal Infotech">
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
    using System;
    using System.Text.RegularExpressions;

    using Toshal.Template.Exceptions;

    /// <summary>
    /// The named token.
    /// </summary>
    public class NamedToken : Token
    {
        #region Constructors and Destructor

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedToken"/> class.
        /// </summary>
        /// <param name="split">
        /// The split.
        /// </param>
        /// <exception cref="TokenMissingNameException">
        /// </exception>
        public NamedToken(Split split)
        {
            this.Attributes = new TokenAttributeDictionary();

            const string tokenExpression = "<%=(?<Name>.*?)%>";
            string tempString = Regex.Match(split.Content, tokenExpression).Groups["Name"].Value.Trim();
            this.Name = GetNameAndAttributes(split, tempString, this.Attributes);
            this.LineNumber = split.LineNumber;
            this.StartingIndex = split.StartingIndex;

            if (string.IsNullOrEmpty(this.Name))
            {
                throw new TokenMissingNameException(split);
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the attributes.
        /// </summary>
        public TokenAttributeDictionary Attributes { get; private set; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// The get name and attributes.
        /// </summary>
        /// <param name="split">
        /// The split.
        /// </param>
        /// <param name="nameString">
        /// The name string.
        /// </param>
        /// <param name="attributes">
        /// The attributes.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        /// <exception cref="InvalidTokenAttributeException">
        /// </exception>
        internal static string GetNameAndAttributes(
            Split split,
            string nameString,
            TokenAttributeDictionary attributes)
        {
            string retVal = nameString;

            if (nameString.IndexOf('=') > -1)
            {
                string[] nameSplit = nameString.Split(' ');
                retVal = nameSplit[0];

                const string tokenAttributeExpression = "(?<Name>\\w+)=\"(?<Value>[^\"]*)\"";

                for (int i = 1; i < nameSplit.Length; i++)
                {
                    Match m = Regex.Match(nameSplit[i], tokenAttributeExpression);
                    if (m.Success == false)
                    {
                        throw new InvalidTokenAttributeException(split);
                    }

                    attributes.Add(m.Groups["Name"].Value.ToLower(), m.Groups["Value"].Value);
                }
            }

            return retVal.Trim().ToLower();
        }

        #endregion

        public string GetAttribute(string attributeName, string defaultValue)
        {
            return Attributes.GetValue(attributeName, defaultValue);
        }
    }
}