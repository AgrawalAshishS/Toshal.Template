// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Toshal.Template.Exceptions;
    using Toshal.Template.Tokens;

    /// <summary>
    /// Turns template text into a list of tokens. Parse a template once and give the tokens to <see cref="Processor.Process(ProcessorArgs)"/>
    /// as often as you like.
    /// </summary>
    /// <remarks>
    /// <para>Tags are written as <c>&lt;%...%&gt;</c>. The keywords (IF, FOREACH, WITH, SET, ...) are upper case and case sensitive.
    /// Names are not case sensitive: the parser lower cases them.</para>
    /// <para>FOREACH names are unique per level. A level is one list of tokens: the top of the template, or the inside of an IF, ELSEIF, ELSE,
    /// WITH, SET or FOREACH part. The same name may be used again at a deeper level, or in the IF part and the ELSE part.
    /// A REUSE_FOREACH uses the nearest FOREACH with its name: first at its own level, then at each outer level up to the top.
    /// When none of those levels has the name, it uses the first FOREACH with that name in the whole template, top to bottom.</para>
    /// <para><b>Warning:</b> a parser keeps state while it works. Parse one template at a time with one instance; do not share an instance between threads.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// List&lt;IToken&gt; tokens = new Parser().Parse("Hello &lt;%=Name%&gt;!");
    /// </code>
    /// </example>
    public class Parser
    {
        #region Public Methods and Operators

        /// <summary>
        /// Parses a template. Plain text becomes <see cref="ContentToken"/>, each tag becomes its token, and blocks such as IF and FOREACH
        /// hold their inner tokens. A REUSE_FOREACH tag is linked to its FOREACH before the method returns.
        /// </summary>
        /// <remarks>
        /// <para>A line that holds only control tags (IF, ELSEIF, ELSE, ENDIF, FOREACH and its parts, ENDFOR, WITH, ENDWITH, SET, ENDSET, SEPARATOR,
        /// ENDSEPARATOR, comments) and spaces
        /// or tabs gives no text token: its indent, the spaces between its tags and its line break are dropped. Lines with text or with a tag that
        /// writes something stay as they are. The line numbers and columns of the tokens stay those of the template.</para>
        /// <para>A PROCESS_TEMPLATE alone on its line after spaces or tabs gets those as its <see cref="ProcessTemplateToken.Indent"/>.</para>
        /// <para>A comment <c>&lt;%-- ... --%&gt;</c> gives no token. It may span lines and hold tags; it ends at the first <c>--%&gt;</c>.
        /// For the line rule above it counts as a control tag. In plain text, <c>\&lt;\%</c> and <c>\%\&gt;</c> become <c>&lt;%</c> and
        /// <c>%&gt;</c> in the <see cref="ContentToken"/>.</para>
        /// </remarks>
        /// <param name="templateText">The template. An empty string gives an empty list.</param>
        /// <returns>The top level tokens, in template order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="templateText"/> is null.</exception>
        /// <exception cref="TokenMissingNameException">A tag that needs a name has none, for example <c>&lt;%=%&gt;</c>.</exception>
        /// <exception cref="TokenNotClosedException">A block has no end tag, for example IF without ENDIF, or a comment has no <c>--%&gt;</c>.</exception>
        /// <exception cref="InvalidTokenAttributeException">The attributes of a tag are not written as <c>name="value"</c>.</exception>
        /// <exception cref="ForEachMissingForReuseException">A REUSE_FOREACH names a FOREACH that is nowhere in the template.</exception>
        /// <exception cref="ParserException">An unknown tag, an end tag without its start, a REMOVE_PREVIOUS count that is missing, not a whole number,
        /// or negative, or two FOREACH blocks with the same name at the same level. All the exceptions above derive from it.</exception>
        /// <example>
        /// <code>
        /// var parser = new Parser();
        /// List&lt;IToken&gt; tokens = parser.Parse("&lt;%IF vip%&gt;Dear &lt;%=Name%&gt;&lt;%ELSE%&gt;Hello&lt;%ENDIF%&gt;");
        /// </code>
        /// </example>
        public List<IToken> Parse(string templateText)
        {
            ArgumentNullException.ThrowIfNull(templateText);

            var retList = new List<IToken>();

            // A parser can be used for many templates. Each template only sees its own FOREACH blocks.
            this._parentLevel.Clear();
            this._forEachByLevel.Clear();
            this._forEachById.Clear();
            this._allReuseForEachTokens.Clear();
            this._levels.Clear();
            this._subTemplateIndents?.Clear();

            this._splits = SplitTemplateByTokens(templateText);
            this.FindSubTemplateIndents(this._splits);
            RemoveTagLines(this._splits);

            this._splitIndex = 0;

            if (this.ProcessSplitsTillEnd(retList) == false)
            {
                throw new ParserException(this._splits[this._splitIndex]);
            }

            this.VerifyAllReuseForEachReferences();

            return retList;
        }

        // Cuts the template into plain text and tags. A split ends before each "<%" and after each "%>"; a "%>" right at the start of a split
        // is plain text. Each split is one contiguous piece of the template, so it is cut with Substring, not built char by char.
        private static List<Split> SplitTemplateByTokens(string templateText)
        {
            var retList = new List<Split>();
            int lineNumber = 1;
            int column = 1;         // 1 based column of templateText[i]
            int start = 0;          // where the current split starts in templateText
            var split = new Split { StartingPosition = column, LineNumber = lineNumber };
            int length = templateText.Length;
            int i = 0;

            while (i < length)
            {
                int found = templateText.AsSpan(i).IndexOfAny('<', '%');
                int next = found < 0 ? length : i + found;

                // Move the line and column over the plain characters in between.
                var between = templateText.AsSpan(i, next - i);
                int lastNewLine = between.LastIndexOf('\n');
                if (lastNewLine < 0)
                {
                    column += between.Length;
                }
                else
                {
                    lineNumber += between.Count('\n');
                    column = between.Length - lastNewLine;
                }

                i = next;
                if (i >= length || i + 1 >= length)
                {
                    break;
                }

                if (templateText[i] == '<' && templateText[i + 1] == '%')
                {
                    if (i > start)
                    {
                        split.Content = templateText.Substring(start, i - start);
                        retList.Add(split);
                    }

                    split = new Split { StartingPosition = column, LineNumber = lineNumber };
                    start = i;

                    // A comment "<%--" ... "--%>" is one split, whatever it holds: tags, "%>" and line breaks too.
                    if (i + 3 < length && templateText[i + 2] == '-' && templateText[i + 3] == '-')
                    {
                        int close = templateText.IndexOf("--%>", i + 4, StringComparison.Ordinal);
                        if (close < 0)
                        {
                            split.Content = templateText.Substring(start);
                            throw new TokenNotClosedException(split, "comment", "<%-- not closed with --%>");
                        }

                        var comment = templateText.AsSpan(i, close + 4 - i);
                        int commentNewLine = comment.LastIndexOf('\n');
                        if (commentNewLine < 0)
                        {
                            column += comment.Length;
                        }
                        else
                        {
                            lineNumber += comment.Count('\n');
                            column = comment.Length - commentNewLine;
                        }

                        i = close + 4;
                        split.Content = templateText.Substring(start, i - start);
                        retList.Add(split);
                        split = new Split { StartingPosition = column, LineNumber = lineNumber };
                        start = i;
                        continue;
                    }

                    i += 2;
                    column += 2;
                    continue;
                }

                if (templateText[i] == '%' && templateText[i + 1] == '>')
                {
                    // "%>" with no open tag and no text before it is plain text.
                    if (i > start)
                    {
                        split.Content = templateText.Substring(start, i + 2 - start);
                        retList.Add(split);
                        split = new Split { StartingPosition = column + 2, LineNumber = lineNumber }; // the text after "%>"
                        start = i + 2;
                    }

                    i += 2;
                    column += 2;
                    continue;
                }

                // A '<' or '%' on its own is plain text.
                i++;
                column++;
            }

            if (length > start)
            {
                split.Content = templateText.Substring(start);
                retList.Add(split);
            }

            return retList;
        }

        #endregion

        #region Fields

        // A level is one token list: the top of the template, or the inside of an IF, ELSEIF, ELSE, WITH, SET or FOREACH part.
        // Each level points to the level around it (null for the top), so REUSE_FOREACH can look from its own level outwards.
        private readonly Dictionary<List<IToken>, List<IToken>?> _parentLevel = new Dictionary<List<IToken>, List<IToken>?>(ReferenceEqualityComparer.Instance);

        // The FOREACH blocks of each level. A name may be used any number of times, at any level.
        private readonly Dictionary<List<IToken>, List<ForEachToken>> _forEachByLevel = new Dictionary<List<IToken>, List<ForEachToken>>(ReferenceEqualityComparer.Instance);

        // The FOREACH blocks that have an id attribute, by lower case id. An id is unique in the whole template.
        private readonly Dictionary<string, ForEachToken> _forEachById = new Dictionary<string, ForEachToken>();

        // Every REUSE_FOREACH with the level it is in. They are linked after the whole template is parsed,
        // so a REUSE_FOREACH may come before its FOREACH.
        private readonly List<(ReuseForEachToken Token, List<IToken> Level)> _allReuseForEachTokens = new List<(ReuseForEachToken Token, List<IToken> Level)>();

        // The levels being filled right now, the innermost on top.
        private readonly Stack<List<IToken>> _levels = new Stack<List<IToken>>();

        /// <summary>
        ///     The _split index.
        /// </summary>
        private int _splitIndex;

        // The indent of each PROCESS_TEMPLATE that stands alone on its line. Made only when a template has one.
        private Dictionary<Split, string>? _subTemplateIndents;

        /// <summary>
        ///     The _splits.
        /// </summary>
        private List<Split> _splits = new List<Split>();

        #endregion

        #region Methods

        // The parts of a FOREACH, in the order the parser has always tried them.
        private static readonly string[] SectionTags =
        {
            "NORECORD", "HEADER", "BEFOREFIRSTROW", "FIRSTROW", "AFTERFIRSTROW", "BEFOREROW", "ROW", "AFTERROW",
            "BEFOREALTROW", "ALTROW", "AFTERALTROW", "BEFORELASTROW", "LASTROW", "AFTERLASTROW", "FOOTER",
        };

        private static readonly Dictionary<string, int> SectionByOpenTag = SectionTags
            .Select((tag, index) => (tag, index))
            .ToDictionary(x => "<%" + x.tag + "%>", x => x.index, StringComparer.Ordinal);

        private static readonly string[] SectionCloseTags = SectionTags.Select(tag => "<%END" + tag + "%>").ToArray();

        private static List<IToken> Section(ForEachToken token, int index) => index switch
        {
            0 => token.NoRecordTokens,
            1 => token.HeaderTokens,
            2 => token.BeforeFirstRowTokens,
            3 => token.FirstRowTokens,
            4 => token.AfterFirstRowTokens,
            5 => token.BeforeRowTokens,
            6 => token.RowTokens,
            7 => token.AfterRowTokens,
            8 => token.BeforeAltRowTokens,
            9 => token.AltRowTokens,
            10 => token.AfterAltRowTokens,
            11 => token.BeforeLastRowTokens,
            12 => token.LastRowTokens,
            13 => token.AfterLastRowTokens,
            _ => token.FooterTokens,
        };

        // A line that holds only control tags and spaces or tabs writes nothing: its indent, the spaces between its tags and its line break go.
        // Tags that write something (values, CONTEXT_AS_STRING, PROCESS_TEMPLATE, REUSE_FOREACH) and REMOVE_PREVIOUS keep their line.
        // Every line is judged on the original text first, then all cuts are made, so two tag lines next to each other do not affect each other.
        private static void RemoveTagLines(List<Split> splits)
        {
            int count = splits.Count;

            // Each split is classified once: text, control tag, or another tag.
            var kinds = new SplitKind[count];
            bool anyControl = false;
            for (int k = 0; k < count; k++)
            {
                kinds[k] = Classify(splits[k].Content);
                anyControl |= kinds[k] == SplitKind.Control;
            }

            if (!anyControl)
            {
                return;
            }

            int[]? cutStart = null;   // characters to cut from the start of a text split
            int[]? cutEnd = null;     // characters to cut from the end of a text split

            for (int i = 0; i < count; i++)
            {
                if (kinds[i] != SplitKind.Control)
                {
                    continue;
                }

                // Find the start of the line: go left over control tags and blank text until a line break or the start of the template.
                int first = i;          // first split of the line that is cut entirely or partly
                int left = i - 1;
                bool standalone = true;
                while (left >= 0)
                {
                    string content = splits[left].Content;
                    if (kinds[left] != SplitKind.Text)
                    {
                        if (kinds[left] != SplitKind.Control) { standalone = false; break; }
                        first = left;
                        left--;
                        continue;
                    }

                    int lastNewLine = content.LastIndexOf('\n');
                    if (!IsBlank(content, lastNewLine + 1, content.Length)) { standalone = false; break; }
                    first = left;
                    if (lastNewLine >= 0) break;
                    left--;
                }

                if (!standalone)
                {
                    continue;
                }

                // Find the end of the line: go right the same way until a line break or the end of the template.
                int last = i;
                int right = i + 1;
                while (right < count)
                {
                    string content = splits[right].Content;
                    if (kinds[right] != SplitKind.Text)
                    {
                        if (kinds[right] != SplitKind.Control) { standalone = false; break; }
                        last = right;
                        right++;
                        continue;
                    }

                    int newLine = content.IndexOf('\n');
                    int end = newLine < 0 ? content.Length : newLine;
                    if (!IsBlank(content, 0, end, allowCarriageReturnAtEnd: newLine >= 0)) { standalone = false; break; }
                    last = right;
                    if (newLine >= 0) break;
                    right++;
                }

                if (!standalone)
                {
                    continue;
                }

                // Cut: the indent after the last line break of the first split, all blank text in between, and the first line break of the last split.
                cutStart ??= new int[count];
                cutEnd ??= new int[count];
                for (int k = first; k <= last; k++)
                {
                    string content = splits[k].Content;
                    if (kinds[k] != SplitKind.Text)
                    {
                        continue;
                    }

                    if (k == first)
                    {
                        int lastNewLine = content.LastIndexOf('\n');
                        if (lastNewLine >= 0)
                        {
                            cutEnd[k] = content.Length - lastNewLine - 1;
                            continue;
                        }
                    }

                    if (k == last)
                    {
                        int newLine = content.IndexOf('\n');
                        if (newLine >= 0)
                        {
                            cutStart[k] = newLine + 1;
                            continue;
                        }
                    }

                    cutStart[k] = content.Length;
                }

                i = last;
            }

            if (cutStart == null || cutEnd == null)
            {
                return;
            }

            // Apply the cuts. A text split that becomes empty is dropped; one that loses its first line starts on the next line, column 1.
            int write = 0;
            for (int k = 0; k < count; k++)
            {
                var split = splits[k];
                if (cutStart[k] > 0 || cutEnd[k] > 0)
                {
                    int start = cutStart[k];
                    int length = split.Content.Length - start - cutEnd[k];
                    if (length <= 0)
                    {
                        continue;
                    }

                    if (start > 0)
                    {
                        split.LineNumber++;
                        split.StartingPosition = 1;
                    }

                    split.Content = split.Content.Substring(start, length);
                }

                splits[write++] = split;
            }

            splits.RemoveRange(write, count - write);
        }

        private static bool IsBlank(string content, int start, int end, bool allowCarriageReturnAtEnd = false)
        {
            if (allowCarriageReturnAtEnd && end > start && content[end - 1] == '\r')
            {
                end--;
            }

            for (int k = start; k < end; k++)
            {
                if (content[k] != ' ' && content[k] != '\t') return false;
            }

            return true;
        }

        private enum SplitKind : byte
        {
            Text,
            Control,
            OtherTag,
        }

        // Control tags only steer the output: blocks, their parts, their ends and comments. The third character picks the few checks to make.
        private static SplitKind Classify(string content)
        {
            if (content.Length < 2 || content[0] != '<' || content[1] != '%')
            {
                return SplitKind.Text;
            }

            if (content.Length < 5 || content[^2] != '%' || content[^1] != '>')
            {
                return SplitKind.OtherTag;
            }

            bool control = content[2] switch
            {
                'I' => content.StartsWith("<%IF ", StringComparison.Ordinal),
                'F' => content.StartsWith("<%FOREACH ", StringComparison.Ordinal) || ControlTags.Contains(content),
                'W' => content.StartsWith("<%WITH ", StringComparison.Ordinal),
                'S' => content.StartsWith("<%SET ", StringComparison.Ordinal) || ControlTags.Contains(content),
                'E' => content.StartsWith("<%ELSEIF ", StringComparison.Ordinal) || ControlTags.Contains(content),
                'N' or 'H' or 'B' or 'R' or 'A' or 'L' => ControlTags.Contains(content),
                '-' => content.StartsWith("<%--", StringComparison.Ordinal),
                _ => false,
            };

            return control ? SplitKind.Control : SplitKind.OtherTag;
        }

        private static readonly HashSet<string> ControlTags = new HashSet<string>(
            SectionTags.Select(tag => "<%" + tag + "%>")
                .Concat(SectionTags.Select(tag => "<%END" + tag + "%>"))
                .Concat(new[] { "<%ELSE%>", "<%ENDIF%>", "<%ENDFOR%>", "<%ENDWITH%>", "<%ENDSET%>", "<%SEPARATOR%>", "<%ENDSEPARATOR%>" }),
            StringComparer.Ordinal);

        // Parses one part of a FOREACH, from its open tag (the current split) to its end tag.
        private void ParseSection(Split split, string name, int section, List<IToken> tokenList)
        {
            var tempSplit = this._splits[this._splitIndex];

            this._splitIndex++;
            if (this.ProcessSplitsTillEnd(tokenList) || this._splits[this._splitIndex].Content != SectionCloseTags[section])
            {
                throw new TokenNotClosedException(tempSplit, name, "<%" + SectionTags[section] + "%> not closed for " + split.Content);
            }

            this._splitIndex++;
        }

        /// <summary>
        ///     The create condition element.
        /// </summary>
        /// <param name="split">
        ///     The split.
        /// </param>
        /// <returns>
        ///     The <see cref="ConditionToken" />.
        /// </returns>
        /// <exception cref="TokenNotClosedException">
        /// </exception>
        private ConditionToken CreateConditionElement(Split split)
        {
            var token = new ConditionToken(split);

            this._splitIndex++;
            if (this.ProcessSplitsTillEnd(token.InnerTokens))
            {
                throw new TokenNotClosedException(split, token.Name);
            }

            if (this._splits[this._splitIndex].Content.StartsWith("<%ELSEIF ", StringComparison.Ordinal))
            {
                token.FalsePart = this.CreateConditionElement(this._splits[this._splitIndex]);
            }
            else
            {
                if (this._splits[this._splitIndex].Content == "<%ELSE%>")
                {
                    token.FalsePart = new ElseToken(this._splits[this._splitIndex]);
                    this._splitIndex++;
                    if (this.ProcessSplitsTillEnd(((ElseToken)token.FalsePart).InnerTokens))
                    {
                        throw new TokenNotClosedException(split, token.Name);
                    }
                }

                if (this._splits[this._splitIndex].Content != "<%ENDIF%>")
                {
                    throw new TokenNotClosedException(split, token.Name);
                }
            }

            return token;
        }

        /// <summary>
        ///     The handled as condition token.
        /// </summary>
        /// <param name="split">
        ///     The split.
        /// </param>
        /// <param name="tokenList">
        ///     The token list.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        private bool HandledAsConditionToken(Split split, List<IToken> tokenList)
        {
            if (split.Content.StartsWith("<%IF ", StringComparison.Ordinal) == false)
            {
                return false;
            }

            var token = this.CreateConditionElement(split);
            tokenList.Add(token);
            return true;
        }

        /// <summary>
        ///     The handled as content token.
        /// </summary>
        /// <param name="split">
        ///     The split.
        /// </param>
        /// <param name="tokenList">
        ///     The token list.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        private bool HandledAsContentToken(Split split, List<IToken> tokenList)
        {
            if (split.Content.StartsWith("<%", StringComparison.Ordinal))
            {
                return false;
            }

            // \<\% and \%\> write <% and %>. The splitter already took them as text; only the content changes.
            if (split.Content.IndexOf('\\') >= 0)
            {
                string content = split.Content.Replace(@"\<\%", "<%").Replace(@"\%\>", "%>");
                split = new Split { Content = content, LineNumber = split.LineNumber, StartingPosition = split.StartingPosition };
            }

            tokenList.Add(new ContentToken(split));
            return true;
        }

        // <%-- ... --%> writes nothing and gives no token.
        private static bool HandledAsComment(Split split)
        {
            return split.Content.StartsWith("<%--", StringComparison.Ordinal);
        }

        /// <summary>
        ///     The handled as for each.
        /// </summary>
        /// <param name="split">
        ///     The split.
        /// </param>
        /// <param name="tokenList">
        ///     The token list.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        /// <exception cref="TokenNotClosedException">
        /// </exception>
        /// <exception cref="ParserException">
        /// </exception>
        private bool HandledAsForEach(Split split, List<IToken> tokenList)
        {
            if (split.Content.StartsWith("<%FOREACH ", StringComparison.Ordinal) == false)
            {
                return false;
            }

            var token = new ForEachToken(split);

            this._splitIndex++;

            int usedSections = 0;   // one bit per part; each part may be used once
            int remaining = SectionTags.Length;

            while (true)
            {
                this.ProcessSplitsTillEnd(token.RowTokens);
                if (this._splitIndex == this._splits.Count)
                {
                    throw new TokenNotClosedException(split, token.Name);
                }

                if (remaining == 0)
                {
                    // all known tokens are parsed - this is unknown token
                    throw new ParserException(this._splits[this._splitIndex]);
                }

                var beforeCount = remaining;

                if (SectionByOpenTag.TryGetValue(this._splits[this._splitIndex].Content, out int section) && (usedSections & (1 << section)) == 0)
                {
                    this.ParseSection(split, token.Name, section, Section(token, section));
                    usedSections |= 1 << section;
                    remaining--;
                }

                if (this._splitIndex == this._splits.Count)
                {
                    throw new TokenNotClosedException(split, token.Name);
                }

                if (this._splits[this._splitIndex].Content == "<%ENDFOR%>")
                {
                    break;
                }

                if (beforeCount == remaining)
                {
                    // none of inner token is matching - this is unknown token
                    throw new ParserException(this._splits[this._splitIndex]);
                }
            }

            // A FOREACH name is the name of its list and may be used any number of times. An id is unique in the whole template.
            if (this._forEachByLevel.TryGetValue(tokenList, out var forEachList) == false)
            {
                forEachList = new List<ForEachToken>();
                this._forEachByLevel[tokenList] = forEachList;
            }

            forEachList.Add(token);

            var id = token.Attributes.GetLowerCaseValue("id", string.Empty);
            if (id.Length > 0 && this._forEachById.TryAdd(id, token) == false)
            {
                // Inner blocks are finished first, so the one found earlier may come later in the text. Report the later one.
                var other = this._forEachById[id];
                var later = StartsBefore(other, token.LineNumber, token.StartingPosition) ? token : other;
                throw new ParserException(later.LineNumber, later.StartingPosition, "FOREACH id " + id + " is used twice. Give each FOREACH its own id.");
            }

            tokenList.Add(token);
            return true;
        }

        /// <summary>
        ///     The handled as named token.
        /// </summary>
        /// <param name="split">
        ///     The split.
        /// </param>
        /// <param name="tokenList">
        ///     The token list.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        private bool HandledAsNamedToken(Split split, List<IToken> tokenList)
        {
            if (split.Content.StartsWith("<%=", StringComparison.Ordinal) == false)
            {
                return false;
            }

            tokenList.Add(new NamedToken(split));
            return true;
        }

        /// <summary>
        ///     The handled as reuse for each.
        /// </summary>
        /// <param name="split">
        ///     The split.
        /// </param>
        /// <param name="tokenList">
        ///     The token list.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        private bool HandledAsReuseForEach(Split split, List<IToken> tokenList)
        {
            if (split.Content.StartsWith("<%REUSE_FOREACH ", StringComparison.Ordinal) == false)
            {
                return false;
            }

            var token = new ReuseForEachToken(split);
            tokenList.Add(token);
            this._allReuseForEachTokens.Add((token, tokenList));
            return true;
        }

        /// <summary>
        ///     The handled as with.
        /// </summary>
        /// <param name="split">
        ///     The split.
        /// </param>
        /// <param name="tokenList">
        ///     The token list.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        /// <exception cref="TokenNotClosedException">
        /// </exception>
        private bool HandledAsWith(Split split, List<IToken> tokenList)
        {
            if (split.Content.StartsWith("<%WITH ", StringComparison.Ordinal) == false)
            {
                return false;
            }

            var token = new WithToken(split);

            this._splitIndex++;
            if (this.ProcessSplitsTillEnd(token.InnerTokens))
            {
                throw new TokenNotClosedException(split, token.Name);
            }

            var lastSplit = this._splits[this._splitIndex];
            if (lastSplit.Content != "<%ENDWITH%>")
            {
                throw new TokenNotClosedException(split, token.Name);
            }

            tokenList.Add(token);
            return true;
        }

        /// <summary>
        ///     The handled as SET tokens.
        /// </summary>
        /// <param name="split">
        ///     The split.
        /// </param>
        /// <param name="tokenList">
        ///     The token list.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        private bool HandledAsSet(Split split, List<IToken> tokenList)
        {
            if (split.Content.StartsWith("<%SET ", StringComparison.Ordinal) == false)
            {
                return false;
            }

            var token = new SetToken(split);

            this._splitIndex++;
            if (this.ProcessSplitsTillEnd(token.InnerTokens))
            {
                throw new TokenNotClosedException(split, token.Name);
            }

            var lastSplit = this._splits[this._splitIndex];
            if (lastSplit.Content != "<%ENDSET%>")
            {
                throw new TokenNotClosedException(split, token.Name);
            }

            tokenList.Add(token);
            return true;
        }

        private bool HandledAsRemovePreviousNewLine(Split split, List<IToken> tokenList)
        {
            if (split.Content != "<%REMOVE_PREVIOUS_NEW_LINE%>")
            {
                return false;
            }

            tokenList.Add(new RemovePreviousNewLineToken(split));

            return true;
        }

        private bool HandledAsRemovePreviousChars(Split split, List<IToken> tokenList)
        {
            if (split.Content.StartsWith("<%REMOVE_PREVIOUS ", StringComparison.Ordinal) == false)
            {
                return false;
            }

            tokenList.Add(new RemovePreviousCharsToken(split));

            return true;
        }

        private bool HandledAsProcessTemplate(Split split, List<IToken> tokenList)
        {
            if (split.Content.StartsWith("<%PROCESS_TEMPLATE ", StringComparison.Ordinal) == false)
            {
                return false;
            }

            var token = new ProcessTemplateToken(split);
            if (this._subTemplateIndents != null && this._subTemplateIndents.TryGetValue(split, out var indent))
            {
                token.Indent = indent;
            }

            tokenList.Add(token);

            return true;
        }

        // <%SEPARATOR%>...<%ENDSEPARATOR%>: written in every row of the innermost FOREACH but the last.
        private bool HandledAsSeparator(Split split, List<IToken> tokenList)
        {
            if (split.Content != "<%SEPARATOR%>")
            {
                return false;
            }

            var token = new SeparatorToken(split);

            this._splitIndex++;
            if (this.ProcessSplitsTillEnd(token.InnerTokens) || this._splits[this._splitIndex].Content != "<%ENDSEPARATOR%>")
            {
                throw new TokenNotClosedException(split, "SEPARATOR");
            }

            tokenList.Add(token);
            return true;
        }

        // A PROCESS_TEMPLATE alone on its line, after spaces or tabs: those spaces or tabs indent every line of the sub template.
        // Found on the original text, before RemoveTagLines changes the text around the tags.
        private void FindSubTemplateIndents(List<Split> splits)
        {
            for (int i = 1; i < splits.Count; i++)
            {
                if (!splits[i].Content.StartsWith("<%PROCESS_TEMPLATE ", StringComparison.Ordinal))
                {
                    continue;
                }

                // Before the tag: the text after the last line break, or the whole first split of the template, must be spaces or tabs.
                string before = splits[i - 1].Content;
                if (before.StartsWith("<%", StringComparison.Ordinal))
                {
                    continue;
                }

                int lastNewLine = before.LastIndexOf('\n');
                if (lastNewLine < 0 && i - 1 != 0)
                {
                    continue;
                }

                if (!IsBlank(before, lastNewLine + 1, before.Length) || lastNewLine + 1 == before.Length)
                {
                    continue;
                }

                // After the tag: nothing, or text that is blank up to its first line break.
                if (i + 1 < splits.Count)
                {
                    string after = splits[i + 1].Content;
                    if (after.StartsWith("<%", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    int newLine = after.IndexOf('\n');
                    if (!IsBlank(after, 0, newLine < 0 ? after.Length : newLine, allowCarriageReturnAtEnd: newLine >= 0))
                    {
                        continue;
                    }
                }

                this._subTemplateIndents ??= new Dictionary<Split, string>(ReferenceEqualityComparer.Instance);
                this._subTemplateIndents[splits[i]] = before.Substring(lastNewLine + 1);
            }
        }

        private bool HandledAsContextAsString(Split split, List<IToken> tokenList)
        {
            if (split.Content != "<%CONTEXT_AS_STRING%>")
            {
                return false;
            }

            tokenList.Add(new ContextAsStringToken(split));

            return true;
        }

        /// <summary>
        ///     The process splits till end.
        /// </summary>
        /// <param name="tokenList">
        ///     The token list.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        private bool ProcessSplitsTillEnd(List<IToken> tokenList)
        {
            // The first time a list is filled, the list being filled around it is its outer level.
            if (this._parentLevel.ContainsKey(tokenList) == false)
            {
                this._parentLevel[tokenList] = this._levels.Count > 0 ? this._levels.Peek() : null;
            }

            this._levels.Push(tokenList);
            try
            {
                return this.ProcessSplitsTillEndOfLevel(tokenList);
            }
            finally
            {
                this._levels.Pop();
            }
        }

        private bool ProcessSplitsTillEndOfLevel(List<IToken> tokenList)
        {
            for (; this._splitIndex < this._splits.Count; this._splitIndex++)
            {
                var split = this._splits[this._splitIndex];

                if (this.HandledAsContentToken(split, tokenList))
                {
                    continue;
                }

                if (this.HandledAsNamedToken(split, tokenList))
                {
                    continue;
                }

                if (HandledAsComment(split))
                {
                    continue;
                }

                if (this.HandledAsConditionToken(split, tokenList))
                {
                    continue;
                }

                if (this.HandledAsForEach(split, tokenList))
                {
                    continue;
                }

                if (this.HandledAsWith(split, tokenList))
                {
                    continue;
                }

                if (this.HandledAsReuseForEach(split, tokenList))
                {
                    continue;
                }

                if (this.HandledAsSet(split, tokenList))
                {
                    continue;
                }

                if (this.HandledAsRemovePreviousNewLine(split, tokenList))
                {
                    continue;
                }

                if (this.HandledAsRemovePreviousChars(split, tokenList))
                {
                    continue;
                }

                if (this.HandledAsProcessTemplate(split, tokenList))
                {
                    continue;
                }

                if (this.HandledAsContextAsString(split, tokenList))
                {
                    continue;
                }

                if (this.HandledAsSeparator(split, tokenList))
                {
                    continue;
                }

                return false; // not reaching to end. Meaning some closing tag.
            }

            return true; // reached to end
        }

        // True when the token starts before the given line and column of the template.
        private static bool StartsBefore(Token token, int lineNumber, int startingPosition) =>
            token.LineNumber < lineNumber || (token.LineNumber == lineNumber && token.StartingPosition < startingPosition);

        /// <summary>
        ///     The verify all reuse for each references.
        /// </summary>
        /// <exception cref="ForEachMissingForReuseException">
        /// </exception>
        private void VerifyAllReuseForEachReferences()
        {
            if (this._allReuseForEachTokens.Count == 0)
            {
                return;
            }

            foreach (var (reuseForEachToken, level) in this._allReuseForEachTokens)
            {
                var name = reuseForEachToken.ExistingForEachName;

                // A FOREACH with that id wins, wherever it is.
                var found = this._forEachById.GetValueOrDefault(name);

                // Then the nearest FOREACH with that name above the REUSE_FOREACH: its own level first, then each outer level up to the top.
                for (List<IToken>? current = level; current != null && found == null; current = this._parentLevel[current])
                {
                    if (this._forEachByLevel.TryGetValue(current, out var forEachList))
                    {
                        foreach (var forEach in forEachList)
                        {
                            if (forEach.Name == name
                                && StartsBefore(forEach, reuseForEachToken.LineNumber, reuseForEachToken.StartingPosition)
                                && (found == null || StartsBefore(found, forEach.LineNumber, forEach.StartingPosition)))
                            {
                                found = forEach;
                            }
                        }
                    }
                }

                // Fallback: the first FOREACH with the name in the whole template, top to bottom. Blocks are finished inner first
                // while parsing, so template order comes from the line and column, not from the order the blocks were found.
                found ??= this._forEachByLevel.Values
                    .SelectMany(forEachList => forEachList)
                    .Where(t => t.Name == name)
                    .OrderBy(t => t.LineNumber)
                    .ThenBy(t => t.StartingPosition)
                    .FirstOrDefault();

                reuseForEachToken.ExistingForEachToken = found
                    ?? throw new ForEachMissingForReuseException(reuseForEachToken.Name, reuseForEachToken.ExistingForEachName, reuseForEachToken.LineNumber, reuseForEachToken.StartingPosition);
            }
        }

        #endregion
    }
}