// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Exceptions
{
    using System;

    /// <summary>
    /// Thrown by <see cref="Parser.Parse(string)"/> when the template text is not valid. It is also the base class of the more specific parser exceptions.
    /// It is thrown as this exact type for an unknown tag (for example <c>&lt;%FOO%&gt;</c>, or an <c>&lt;%ENDIF%&gt;</c> without IF),
    /// an unknown block inside a FOREACH, and a <c>&lt;%REMOVE_PREVIOUS n%&gt;</c> whose n is not a whole number or is negative.
    /// </summary>
    /// <example>
    /// <code>
    /// try { new Parser().Parse(text); }
    /// catch (ParserException ex) { Console.WriteLine($"Line {ex.LineNumber}, column {ex.StartingPosition}: {ex.Message}"); }
    /// </code>
    /// </example>
    public class ParserException : Exception
    {
        /// <summary>
        /// Creates the exception for a piece of the template, with a message.
        /// </summary>
        /// <param name="split">The piece that failed. Its line and column are copied.</param>
        /// <param name="message">The message.</param>
        /// <example>
        /// <code>
        /// throw new ParserException(split, "Char count is missing or not integer");
        /// </code>
        /// </example>
        public ParserException(Split split, string message)
            : base(message)
        {
            Split = split;
            this.LineNumber = split.LineNumber;
            this.StartingPosition = split.StartingPosition;
        }

        /// <summary>
        /// Creates the exception for a piece of the template with the message "Token - (text) is wrong @(column) on line number (line)".
        /// </summary>
        /// <param name="split">The piece that failed. Its line and column are copied.</param>
        /// <example>
        /// <code>
        /// throw new ParserException(split);
        /// </code>
        /// </example>
        public ParserException(Split split)
            : this(split, "Token - " + split.Content + " is wrong @" + split.StartingPosition + " on line number " + split.LineNumber)
        {
        }

        /// <summary>
        /// Creates the exception from a position and a message, without a piece. <see cref="Split"/> stays null.
        /// </summary>
        /// <param name="lineNumber">The 1 based line.</param>
        /// <param name="startingPosition">The 1 based column.</param>
        /// <param name="message">The message.</param>
        /// <example>
        /// <code>
        /// throw new ParserException(3, 10, "Something is wrong here");
        /// </code>
        /// </example>
        public ParserException(int lineNumber, int startingPosition, string message)
            : base(message)
        {
            this.LineNumber = lineNumber;
            this.StartingPosition = startingPosition;
        }

        /// <summary>
        /// Gets or sets the piece of the template that failed. It is null when the exception was made from a position only,
        /// as <see cref="ForEachMissingForReuseException"/> is.
        /// </summary>
        /// <example>
        /// <code>
        /// string badText = ex.Split?.Content ?? "";
        /// </code>
        /// </example>
        public Split? Split { get; set; }

        /// <summary>
        /// Gets the 1 based line where the failing piece starts.
        /// </summary>
        /// <example>
        /// <code>
        /// int line = ex.LineNumber;
        /// </code>
        /// </example>
        public int LineNumber { get; private set; }

        /// <summary>
        /// Gets the 1 based column where the failing piece starts.
        /// </summary>
        /// <example>
        /// <code>
        /// int column = ex.StartingPosition;
        /// </code>
        /// </example>
        public int StartingPosition { get; private set; }
    }
}
