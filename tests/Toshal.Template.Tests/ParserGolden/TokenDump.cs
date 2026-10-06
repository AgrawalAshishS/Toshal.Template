using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Toshal.Template.Exceptions;
using Toshal.Template.Tokens;

namespace Toshal.Template.Tests.ParserGolden
{
    // Writes everything the parser produces as text: every token with its position, name, attributes and children,
    // or the exception with its position and message. Two parsers that give the same text behave the same.
    public static class TokenDump
    {
        public static string Parse(string template)
        {
            try
            {
                var tokens = new Parser().Parse(template);
                var text = new StringBuilder();
                Write(text, tokens, 0);
                return text.ToString();
            }
            catch (ParserException ex)
            {
                return $"{ex.GetType().Name} @{ex.LineNumber}:{ex.StartingPosition} {Escape(ex.Message)}\n";
            }
        }

        private static void Write(StringBuilder text, List<IToken> tokens, int depth)
        {
            foreach (var token in tokens)
            {
                text.Append(' ', depth * 2).Append(Line(token)).Append('\n');
                switch (token)
                {
                    case ConditionToken condition:
                        Write(text, condition.InnerTokens, depth + 1);
                        WriteFalsePart(text, condition.FalsePart, depth);
                        break;
                    case ForEachToken forEach:
                        foreach (var (name, list) in Sections(forEach))
                        {
                            if (list.Count == 0) continue;
                            text.Append(' ', depth * 2 + 2).Append('[').Append(name).Append("]\n");
                            Write(text, list, depth + 2);
                        }

                        break;
                    case ContainerTokenBase container:
                        Write(text, container.InnerTokens, depth + 1);
                        break;
                }
            }
        }

        private static void WriteFalsePart(StringBuilder text, IContainerToken? falsePart, int depth)
        {
            switch (falsePart)
            {
                case null:
                    return;
                case ElseToken elseToken:
                    text.Append(' ', depth * 2).Append($"ELSE @{elseToken.LineNumber}:{elseToken.StartingPosition}\n");
                    Write(text, elseToken.InnerTokens, depth + 1);
                    return;
                case ConditionToken elseIf:
                    text.Append(' ', depth * 2).Append("ELSE").Append(Line(elseIf)).Append('\n');
                    Write(text, elseIf.InnerTokens, depth + 1);
                    WriteFalsePart(text, elseIf.FalsePart, depth);
                    return;
            }
        }

        private static string Line(IToken token)
        {
            string at = $"@{token.LineNumber}:{token.StartingPosition}";
            return token switch
            {
                ContentToken t => $"TEXT {at} {Escape(t.Content)}",
                NamedToken t => $"VALUE {at} {Escape(t.Name)}{Attributes(t.Attributes)}",
                ConditionToken t => $"IF {at} {(t.IsPositive ? "" : "not ")}{Escape(t.Name)}{Attributes(t.Attributes)}",
                ForEachToken t => $"FOREACH {at} {Escape(t.Name)}{Attributes(t.Attributes)}",
                WithToken t => $"WITH {at} {Escape(t.Name)}{Attributes(t.Attributes)}",
                SetToken t => $"SET {at} {Escape(t.Name)}{Attributes(t.Attributes)}",
                ReuseForEachToken t => $"REUSE {at} {Escape(t.ExistingForEachName)} {Escape(t.Name)} -> {(t.ExistingForEachToken == null ? "none" : $"@{t.ExistingForEachToken.LineNumber}:{t.ExistingForEachToken.StartingPosition}")}",
                ProcessTemplateToken t => $"PROCESS_TEMPLATE {at} {Escape(t.Name)}{Attributes(t.Attributes)}{(t.Indent.Length > 0 ? " indent " + Escape(t.Indent) : "")}",
                SeparatorToken => $"SEPARATOR {at}",
                RemovePreviousCharsToken t => $"REMOVE_PREVIOUS {at} {t.CharCount}",
                RemovePreviousNewLineToken => $"REMOVE_PREVIOUS_NEW_LINE {at}",
                ContextAsStringToken => $"CONTEXT_AS_STRING {at}",
                ElseToken => $"ELSE {at}",
                _ => $"{token.GetType().Name} {at}",
            };
        }

        private static string Attributes(TokenAttributeDictionary attributes)
        {
            if (attributes.Count == 0 && attributes.LowerCaseValues.Count == 0) return "";
            var all = attributes.Select(a => $"{Escape(a.Key)}={Escape(a.Value)}");
            var lower = attributes.LowerCaseValues.Select(a => $"{Escape(a.Key)}={Escape(a.Value)}");
            return " {" + string.Join(", ", all) + " | " + string.Join(", ", lower) + "}";
        }

        private static IEnumerable<(string Name, List<IToken> List)> Sections(ForEachToken t)
        {
            yield return ("NORECORD", t.NoRecordTokens);
            yield return ("HEADER", t.HeaderTokens);
            yield return ("BEFOREFIRSTROW", t.BeforeFirstRowTokens);
            yield return ("FIRSTROW", t.FirstRowTokens);
            yield return ("AFTERFIRSTROW", t.AfterFirstRowTokens);
            yield return ("BEFOREROW", t.BeforeRowTokens);
            yield return ("ROW", t.RowTokens);
            yield return ("AFTERROW", t.AfterRowTokens);
            yield return ("BEFOREALTROW", t.BeforeAltRowTokens);
            yield return ("ALTROW", t.AltRowTokens);
            yield return ("AFTERALTROW", t.AfterAltRowTokens);
            yield return ("BEFORELASTROW", t.BeforeLastRowTokens);
            yield return ("LASTROW", t.LastRowTokens);
            yield return ("AFTERLASTROW", t.AfterLastRowTokens);
            yield return ("FOOTER", t.FooterTokens);
        }

        // Shows every control and non ASCII character, so whitespace differences are visible.
        public static string Escape(string text)
        {
            var result = new StringBuilder("\"");
            foreach (var c in text)
            {
                if (c == '"' || c == '\\') result.Append('\\').Append(c);
                else if (c < 0x20 || c > 0x7E) result.Append("\\u").Append(((int)c).ToString("X4"));
                else result.Append(c);
            }

            return result.Append('"').ToString();
        }
    }
}
