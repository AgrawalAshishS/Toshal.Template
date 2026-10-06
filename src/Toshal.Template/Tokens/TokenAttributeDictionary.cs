// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Tokens
{
    using Exceptions;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    /// The attributes of a tag, such as <c>format="0.00"</c> in <c>&lt;%=Total format="0.00"%&gt;</c>. Keys are the attribute names in lower case.
    /// A quoted value may contain spaces.
    /// </summary>
    /// <remarks>
    /// <para><b>Warning:</b> it is a normal, case sensitive dictionary with lower case keys. Use <see cref="GetValue(string, string)"/>, or read it with
    /// a lower case key: <c>attributes["format"]</c> works, <c>attributes["Format"]</c> throws KeyNotFoundException.</para>
    /// <para>The same attribute twice in one tag makes the parser throw InvalidTokenAttributeException.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var token = (NamedToken)new Parser().Parse("&lt;%=Total format=\"0.00\"%&gt;")[0];
    /// string format = token.Attributes.GetValue("Format", "0");   // "0.00"
    /// </code>
    /// </example>
    public class TokenAttributeDictionary : Dictionary<string, string>
    {
        /// <summary>
        /// Gets an attribute value, or a default when the attribute is missing. The name is not case sensitive.
        /// </summary>
        /// <param name="attributeName">The attribute name. Must not be null.</param>
        /// <param name="defaultValue">The value to return when the attribute is missing.</param>
        /// <returns>The attribute value, or <paramref name="defaultValue"/>.</returns>
        /// <example>
        /// <code>
        /// string format = attributes.GetValue("FORMAT", "0");
        /// </code>
        /// </example>
        public string GetValue(string attributeName, string defaultValue)
        {
            attributeName = attributeName.ToLower();
            var retVal = defaultValue;
            if (this.TryGetValue(attributeName, out retVal)) return retVal;
            return defaultValue;
        }



        /// <summary>
        /// Splits "name attr="value" ..." into the name and the attributes.
        /// </summary>
        /// <param name="split">The tag, for the exception.</param>
        /// <param name="nameString">The text after the tag keyword.</param>
        /// <param name="attributes">The dictionary to fill.</param>
        /// <returns>The name, trimmed and in lower case.</returns>
        /// <exception cref="InvalidTokenAttributeException">The attributes are not written as <c>name="value"</c>.</exception>
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
                    // The same attribute twice is an error, not a choice between the two values.
                    if (attributes.TryAdd(m.Groups["Name"].Value.ToLower(), m.Groups["Value"].Value) == false)
                    {
                        throw new InvalidTokenAttributeException(split);
                    }
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