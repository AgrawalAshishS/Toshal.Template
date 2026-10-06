// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Tokens
{
    using Exceptions;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    /// <summary>
    /// The attributes of a tag, such as <c>format="0.00"</c> in <c>&lt;%=Total format="0.00"%&gt;</c>. Keys are the attribute names in lower case.
    /// Values keep the case the template author wrote, in every tag. A quoted value may contain spaces.
    /// </summary>
    /// <remarks>
    /// <para>Case rules: the names the library itself uses (the tag name, such as <c>total</c>, and the FOREACH name of a REUSE_FOREACH) and the
    /// attribute names are lower case. Attribute values are your own settings, so they keep their case. When you want to compare a value without case,
    /// use <see cref="LowerCaseValues"/> or <see cref="GetLowerCaseValue(string, string)"/>: the parser fills a lower case copy of every value
    /// while it parses, so nothing is lower cased again for each run of the processor.</para>
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
        // Made only for a tag that has attributes, so most tokens carry one dictionary, not two.
        private Dictionary<string, string>? lowerCaseValues;

        /// <summary>
        /// Gets the same attributes with the values in lower case (invariant culture), for example <c>n2</c> for <c>format="N2"</c>.
        /// The parser fills it together with the dictionary itself.
        /// </summary>
        /// <remarks>
        /// <para><b>Warning:</b> only the parser fills this copy. Entries that you add to the dictionary yourself, with <c>Add</c> or the indexer,
        /// are not in it.</para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // &lt;%IF Weight unit="KG"%&gt;
        /// bool kilograms = args.Attributes.LowerCaseValues.TryGetValue("unit", out var unit) &amp;&amp; unit == "kg";
        /// </code>
        /// </example>
        public IReadOnlyDictionary<string, string> LowerCaseValues => (IReadOnlyDictionary<string, string>?)this.lowerCaseValues ?? ReadOnlyDictionary<string, string>.Empty;

        /// <summary>
        /// Gets an attribute value, or a default when the attribute is missing. The name is not case sensitive.
        /// </summary>
        /// <param name="attributeName">The attribute name.</param>
        /// <param name="defaultValue">The value to return when the attribute is missing.</param>
        /// <returns>The attribute value, or <paramref name="defaultValue"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="attributeName"/> is null.</exception>
        /// <example>
        /// <code>
        /// string format = attributes.GetValue("FORMAT", "0");
        /// </code>
        /// </example>
        public string GetValue(string attributeName, string defaultValue)
        {
            ArgumentNullException.ThrowIfNull(attributeName);

            return this.TryGetValue(attributeName.ToLower(), out var value) ? value : defaultValue;
        }

        /// <summary>
        /// Gets an attribute value in lower case, or a default when the attribute is missing. The name is not case sensitive.
        /// The value comes from <see cref="LowerCaseValues"/>, which the parser fills.
        /// </summary>
        /// <param name="attributeName">The attribute name.</param>
        /// <param name="defaultValue">The value to return when the attribute is missing. It is returned as given, not lower cased.</param>
        /// <returns>The attribute value in lower case, or <paramref name="defaultValue"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="attributeName"/> is null.</exception>
        /// <example>
        /// <code>
        /// // &lt;%=Total Currency="INR"%&gt;
        /// string currency = attributes.GetLowerCaseValue("currency", "usd");   // "inr"
        /// </code>
        /// </example>
        public string GetLowerCaseValue(string attributeName, string defaultValue)
        {
            ArgumentNullException.ThrowIfNull(attributeName);

            string? value = null;
            return this.lowerCaseValues?.TryGetValue(attributeName.ToLower(), out value) == true ? value! : defaultValue;
        }



        /// <summary>
        /// Splits "name attr="value" ..." into the name and the attributes.
        /// </summary>
        /// <param name="split">The tag, for the exception.</param>
        /// <param name="nameString">The text after the tag keyword.</param>
        /// <param name="attributes">The dictionary to fill.</param>
        /// <returns>The name, trimmed and in lower case. Attribute names are lower cased, attribute values keep their case.</returns>
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

                // Attributes follow each other: name="value". A quoted value may contain spaces.
                // The loop reads what the regular expression \G\s*(?<Name>\w+)="(?<Value>[^"]*)" matched, one attribute after the other.
                int position = firstSpace;
                bool any = false;
                while (true)
                {
                    int p = position;
                    while (p < nameString.Length && char.IsWhiteSpace(nameString[p])) p++;

                    int keyStart = p;
                    while (p < nameString.Length && TagText.IsWordChar(nameString[p])) p++;
                    if (p == keyStart || p + 1 >= nameString.Length || nameString[p] != '=' || nameString[p + 1] != '"') break;

                    int valueStart = p + 2;
                    int valueEnd = nameString.IndexOf('"', valueStart);
                    if (valueEnd < 0) break;

                    // The same attribute twice is an error, not a choice between the two values.
                    string key = nameString.Substring(keyStart, p - keyStart).ToLower();
                    string value = nameString.Substring(valueStart, valueEnd - valueStart);
                    if (attributes.TryAdd(key, value) == false)
                    {
                        throw new InvalidTokenAttributeException(split);
                    }

                    // The lower case copy is made once here, at parse time.
                    (attributes.lowerCaseValues ??= new Dictionary<string, string>()).Add(key, value.ToLowerInvariant());
                    position = valueEnd + 1;
                    any = true;
                }

                if (!any || !nameString.AsSpan(position).Trim().IsEmpty)
                {
                    throw new InvalidTokenAttributeException(split);
                }
            }

            return retVal.Trim().ToLower();
        }

    }
}
