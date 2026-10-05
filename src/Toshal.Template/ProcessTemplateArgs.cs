// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProcessTemplateArgs.cs" company="Toshal Infotech">
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
    using Template.Tokens;

    /// <summary>
    /// The token args.
    /// </summary>
    public class ProcessTemplateArgs : ArgsBase
    {
        #region Constructors and Destructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessTemplateArgs"/> class.
        /// </summary>
        /// <param name="token">
        /// The token.
        /// </param>
        /// <param name="context">
        /// The context.
        /// </param>
        public ProcessTemplateArgs(ProcessTemplateToken token, object? context, List<object?> parentContext)
            : base(token.Name, context, parentContext)
        {
            this.Attributes = token.Attributes;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the attributes.
        /// </summary>
        public TokenAttributeDictionary Attributes { get; private set; }

        #endregion

        public string GetAttribute(string key, string defaultValue)
        {
            return Attributes.GetValue(key, defaultValue);
        }
    }
}