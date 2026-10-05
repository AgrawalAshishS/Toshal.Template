// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TokenAttributeDictionary.cs" company="Toshal Infotech">
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
    using Exceptions;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    ///     The token attribute dictionary.
    /// </summary>
    public class TokenAttributeDictionary : Dictionary<string, string>
    {
        public string GetValue(string attributeName, string defaultValue)
        {
            attributeName = attributeName.ToLower();
            var retVal = defaultValue;
            if (this.TryGetValue(attributeName, out retVal)) return retVal;
            return defaultValue;
        }



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

            int firstSpace = nameString.IndexOf(' ');
            if (nameString.IndexOf('=') > -1 && firstSpace > -1)
            {
                retVal = nameString.Substring(0, firstSpace);
                string rest = nameString.Substring(firstSpace);

                // Attributes follow each other: name="value". A quoted value may contain spaces.
                const string tokenAttributeExpression = "\\G\\s*(?<Name>\\w+)=\"(?<Value>[^\"]*)\"";

                int end = 0;
                foreach (Match m in Regex.Matches(rest, tokenAttributeExpression))
                {
                    attributes.Add(m.Groups["Name"].Value.ToLower(), m.Groups["Value"].Value);
                    end = m.Index + m.Length;
                }

                if (end == 0 || rest.Substring(end).Trim().Length > 0)
                {
                    throw new InvalidTokenAttributeException(split);
                }
            }

            return retVal.Trim().ToLower();
        }

    }
}