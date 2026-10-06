// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Tokens
{

    using Toshal.Template.Exceptions;

    /// <summary>
    /// A parsed <c>&lt;%IF name%&gt;</c> or <c>&lt;%ELSEIF name%&gt;</c> tag with the tokens of its true part.
    /// The word <c>THEN</c> at the end is optional: <c>&lt;%IF paid THEN%&gt;</c> is the same as <c>&lt;%IF paid%&gt;</c>.
    /// <c>&lt;%IF not paid%&gt;</c> is a negative condition.
    /// </summary>
    /// <example>
    /// <code>
    /// var token = (ConditionToken)new Parser().Parse("&lt;%IF not Paid%&gt;Please pay&lt;%ELSE%&gt;Thanks&lt;%ENDIF%&gt;")[0];
    /// // token.Name == "paid", token.IsPositive == false, token.FalsePart is an ElseToken
    /// </code>
    /// </example>
    public sealed class ConditionToken : ContainerTokenBase
    {
        /// <summary>
        /// Reads the name, the <c>not</c> and the attributes from an IF or ELSEIF tag. The parser calls it.
        /// </summary>
        /// <param name="split">The tag. If its content starts with <c>&lt;%IF</c> it is read as IF, otherwise as ELSEIF.</param>
        /// <exception cref="TokenMissingNameException">The tag has no name, for example <c>&lt;%IF %&gt;</c>.</exception>
        /// <exception cref="InvalidTokenAttributeException">The attributes are not written as <c>name="value"</c>.</exception>
        /// <example>
        /// <code>
        /// var token = new ConditionToken(new Split { Content = "&lt;%IF paid%&gt;" });
        /// </code>
        /// </example>
        public ConditionToken(Split split)
        {
            this.Name = string.Empty;
            this.IsPositive = true;
            this.LineNumber = split.LineNumber;
            this.StartingPosition = split.StartingPosition;

            // <%IF name THEN%> first, then <%IF name%>; the same for ELSEIF.
            string keyword = split.Content.StartsWith("<%IF", StringComparison.Ordinal) ? "<%IF" : "<%ELSEIF";
            this.Name = TagText.NameBeforeThen(split.Content, keyword).Trim();
            if (string.IsNullOrEmpty(this.Name))
            {
                this.Name = TagText.Name(split.Content, keyword, space: true, minLength: 0).Trim();
            }

            if (string.IsNullOrEmpty(this.Name))
            {
                throw new TokenMissingNameException(split);
            }


            // "not" is a keyword, so it is found in any case. The name and the attribute names are lower cased by GetNameAndAttributes;
            // attribute values keep their case.
            if (Name.StartsWith("not ", StringComparison.OrdinalIgnoreCase))
            {
                this.IsPositive = false;
                Name = Name.Substring(4);
            }

            this.Name = TokenAttributeDictionary.GetNameAndAttributes(split, Name, this.Attributes);
        }

        /// <summary>
        /// Gets or sets what runs when the condition does not hold: a <see cref="ConditionToken"/> for ELSEIF, an <see cref="ElseToken"/> for ELSE,
        /// or null when there is neither.
        /// </summary>
        /// <example>
        /// <code>
        /// if (token.FalsePart is ElseToken elseToken) { /* the ELSE part */ }
        /// </code>
        /// </example>
        public IContainerToken? FalsePart { get; set; }

        /// <summary>
        /// Gets or sets whether the condition is positive. It is false for <c>&lt;%IF not name%&gt;</c>.
        /// The processor runs the true part when the condition value provider returns this value.
        /// </summary>
        /// <example>
        /// <code>
        /// bool negative = !token.IsPositive;
        /// </code>
        /// </example>
        public bool IsPositive { get; set; }

        /// <summary>
        /// Gets the condition name in lower case, without <c>not</c>, <c>THEN</c> and the attributes.
        /// </summary>
        /// <example>
        /// <code>
        /// string name = token.Name;   // "paid" for &lt;%IF not Paid THEN%&gt;
        /// </code>
        /// </example>
        public string Name { get; }

        /// <summary>
        /// Gets the attributes of the tag. Keys are lower case.
        /// </summary>
        /// <remarks>
        /// <para>Attribute names are lower case; values keep the case the template author wrote. For a lower case copy of the values use <see cref="TokenAttributeDictionary.LowerCaseValues"/>.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// string min = token.Attributes.GetValue("min", "0");
        /// </code>
        /// </example>
        public TokenAttributeDictionary Attributes { get; private set; } = new TokenAttributeDictionary();
    }
}
