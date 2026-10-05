// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Tokens
{
    /// <summary>
    /// One parsed part of a template: plain text or a tag. <see cref="Parser.Parse(string)"/> returns a list of them,
    /// and <see cref="Processor.Process(ProcessorArgs)"/> turns the list into text.
    /// </summary>
    /// <example>
    /// <code>
    /// foreach (IToken token in new Parser().Parse(text))
    ///     Console.WriteLine($"{token.GetType().Name} at line {token.LineNumber}, column {token.StartingPosition}");
    /// </code>
    /// </example>
    public interface IToken
    {
        /// <summary>
        /// Gets or sets the 1 based column where the token starts in the template.
        /// </summary>
        /// <example>
        /// <code>
        /// int column = token.StartingPosition;
        /// </code>
        /// </example>
        int StartingPosition { get; set; }

        /// <summary>
        /// Gets or sets the 1 based line where the token starts in the template.
        /// </summary>
        /// <example>
        /// <code>
        /// int line = token.LineNumber;
        /// </code>
        /// </example>
        int LineNumber { get; set; }
    }
}
