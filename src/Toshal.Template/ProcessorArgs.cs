// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProcessorArgs.cs" company="Toshal Infotech">
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

    using Toshal.Template.Tokens;

    /// <summary>
    ///     The processor args.
    /// </summary>
    public class ProcessorArgs
    {
        #region Constructors and Destructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="ProcessorArgs" /> class.
        /// </summary>
        /// <param name="tokenList">
        ///     The token list.
        /// </param>
        public ProcessorArgs(List<IToken> tokenList)
        {
            this.TokenList = tokenList;
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets the context.
        /// </summary>
        public object Context { get; set; }

        /// <summary>
        ///     Gets the token list.
        /// </summary>
        public List<IToken> TokenList { get; private set; }

        #endregion
    }
}