// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Tokens
{
    using System;

    // Reads the parts of a tag without Regex. Each method gives the same result as the regular expression in its comment
    // gave before: the leftmost match, '.' never matches '\n', '\s' is char.IsWhiteSpace, and a lazy part takes as little as it can.
    internal static class TagText
    {
        // Regex.Match(text, prefix + (space ? @"\s" : "") + "(?<Name>.*?)%>")  with ".+?" when minLength is 1.
        public static string Name(string text, string prefix, bool space, int minLength)
        {
            for (var s = text.IndexOf(prefix, StringComparison.Ordinal); s >= 0; s = text.IndexOf(prefix, s + 1, StringComparison.Ordinal))
            {
                var start = s + prefix.Length;
                if (space)
                {
                    if (start >= text.Length || !char.IsWhiteSpace(text[start])) continue;
                    start++;
                }

                var end = FindClose(text, start, minLength);
                if (end >= 0) return text.Substring(start, end - start);
            }

            return string.Empty;
        }

        // Regex.Match(text, prefix + @"\s(?<Name>.*?)\sTHEN%>")
        public static string NameBeforeThen(string text, string prefix)
        {
            for (var s = text.IndexOf(prefix, StringComparison.Ordinal); s >= 0; s = text.IndexOf(prefix, s + 1, StringComparison.Ordinal))
            {
                var start = s + prefix.Length;
                if (start >= text.Length || !char.IsWhiteSpace(text[start])) continue;
                start++;

                for (var e = start; e < text.Length; e++)
                {
                    if (char.IsWhiteSpace(text[e]) && text.AsSpan(e + 1).StartsWith("THEN%>", StringComparison.Ordinal))
                    {
                        return text.Substring(start, e - start);
                    }

                    if (text[e] == '\n') break;
                }
            }

            return string.Empty;
        }

        // Regex.Match(text, @"<%REUSE_FOREACH\s(?<ExistingForEachName>.+?)\s(?<Name>.+?)%>")
        public static (string Existing, string Name) ReuseNames(string text)
        {
            const string prefix = "<%REUSE_FOREACH";
            for (var s = text.IndexOf(prefix, StringComparison.Ordinal); s >= 0; s = text.IndexOf(prefix, s + 1, StringComparison.Ordinal))
            {
                var start = s + prefix.Length;
                if (start >= text.Length || !char.IsWhiteSpace(text[start])) continue;
                start++;

                for (var e = start; e < text.Length; e++)
                {
                    if (e > start && char.IsWhiteSpace(text[e]))
                    {
                        var end = FindClose(text, e + 1, 1);
                        if (end >= 0) return (text.Substring(start, e - start), text.Substring(e + 1, end - e - 1));
                    }

                    if (text[e] == '\n') break;
                }
            }

            return (string.Empty, string.Empty);
        }

        // Regex \w: letters, non spacing marks, decimal digits and connector punctuation.
        public static bool IsWordChar(char c)
        {
            if (c < 128) return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';

            switch (char.GetUnicodeCategory(c))
            {
                case System.Globalization.UnicodeCategory.UppercaseLetter:
                case System.Globalization.UnicodeCategory.LowercaseLetter:
                case System.Globalization.UnicodeCategory.TitlecaseLetter:
                case System.Globalization.UnicodeCategory.ModifierLetter:
                case System.Globalization.UnicodeCategory.OtherLetter:
                case System.Globalization.UnicodeCategory.NonSpacingMark:
                case System.Globalization.UnicodeCategory.DecimalDigitNumber:
                case System.Globalization.UnicodeCategory.ConnectorPunctuation:
                    return true;
                default:
                    return false;
            }
        }

        // The first e with at least minLength characters before it, where "%>" starts, and no '\n' between start and e; -1 when there is none.
        private static int FindClose(string text, int start, int minLength)
        {
            for (var e = start; e + 1 < text.Length; e++)
            {
                if (e - start >= minLength && text[e] == '%' && text[e + 1] == '>') return e;
                if (text[e] == '\n') return -1;
            }

            return -1;
        }
    }
}
