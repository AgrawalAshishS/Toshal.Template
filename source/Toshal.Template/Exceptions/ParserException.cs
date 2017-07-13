// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ParserException.cs" company="Toshal Infotech">
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

namespace Toshal.Template.Exceptions
{
    using System;

    /// <summary>
    /// The parser exception.
    /// </summary>
    public class ParserException : Exception
    {
        #region Constructors and Destructor

        public ParserException(Split split, string message)
            : base(message)
        {
            Split = split;
            this.LineNumber = split.LineNumber;
            this.StartingIndex = split.StartingIndex;
        }

        public ParserException(Split split)
            : this(split, "Token - " + split.Content + " is wrong @" + split.StartingIndex + " on line number " + split.LineNumber)
        {
        }

        public ParserException(int lineNumber, int startingIndex, string message)
            : base(message)
        {
            this.LineNumber = lineNumber;
            this.StartingIndex = startingIndex;
        }
        
        #endregion

        #region Public Properties

        public Split Split { get; set; }

        public int LineNumber { get; private set; }

        public int StartingIndex { get; private set; }

        #endregion
    }
}