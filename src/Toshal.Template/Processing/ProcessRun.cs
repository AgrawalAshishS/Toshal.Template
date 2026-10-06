// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.Processing
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using Toshal.Template.Tokens;

    // The state of one call of Process: the stack of parent contexts, and one TokenArgs and one ConditionArgs that every provider call reuses.
    // A new run per call keeps one Processor (or one compiled template) safe to use from many threads, and lets a provider call Process again.
    // Processor and the compiled templates of Toshal.Template.Compiled share it, so both write the same text.
    internal sealed class ProcessRun
    {
        private TokenArgs? tokenArgs;
        private ConditionArgs? conditionArgs;

        // Builders for SET values. A SET inside a SET value needs its own, so they are kept as a stack.
        private StringBuilder[] builders = Array.Empty<StringBuilder>();
        private int buildersInUse;

        private readonly StringBuilder output;
        private readonly int outputStart;

        public ProcessRun(List<object?> parentContext, StringBuilder output)
        {
            this.ParentContext = parentContext;
            this.output = output;
            this.outputStart = output.Length;
        }

        // Where the text of this call starts in a builder: after the text the caller had in the main output, at 0 in a SET value.
        public int StartOf(StringBuilder builder) => ReferenceEquals(builder, this.output) ? this.outputStart : 0;

        // One state per FOREACH being processed, the innermost last: not in a row, in a row before the last, or in the last row.
        private const byte NotInRow = 0;
        private const byte RowBeforeTheLast = 1;
        private const byte LastRow = 2;
        private byte[] loopStates = Array.Empty<byte>();
        private int loopDepth;

        // True while a row of the innermost FOREACH is processed and it is not the last row: SEPARATOR blocks are written.
        public bool InRowBeforeTheLast => this.loopDepth > 0 && this.loopStates[this.loopDepth - 1] == RowBeforeTheLast;

        public void EnterLoop()
        {
            if (this.loopDepth == this.loopStates.Length)
            {
                Array.Resize(ref this.loopStates, this.loopStates.Length + 4);
            }

            this.loopStates[this.loopDepth++] = NotInRow;
        }

        public void SetRow(bool isLast) => this.loopStates[this.loopDepth - 1] = isLast ? LastRow : RowBeforeTheLast;

        public void LeaveRow() => this.loopStates[this.loopDepth - 1] = NotInRow;

        public void ExitLoop() => this.loopDepth--;

        public List<object?> ParentContext { get; }

        public TokenArgs TokenArgs(NamedToken token, object? context)
        {
            return this.tokenArgs == null
                ? this.tokenArgs = new TokenArgs(token, context, this.ParentContext)
                : this.tokenArgs.Reuse(token.Name, token.Attributes, context);
        }

        public TokenArgs TokenArgs(WithToken token, object? context)
        {
            return this.tokenArgs == null
                ? this.tokenArgs = new TokenArgs(token, context, this.ParentContext)
                : this.tokenArgs.Reuse(token.Name, token.Attributes, context);
        }

        public TokenArgs TokenArgs(string name, TokenAttributeDictionary attributes, object? context)
        {
            return this.tokenArgs == null
                ? this.tokenArgs = new TokenArgs(name, attributes, context, this.ParentContext)
                : this.tokenArgs.Reuse(name, attributes, context);
        }

        public ConditionArgs ConditionArgs(ConditionToken token, object? context)
        {
            return this.conditionArgs == null
                ? this.conditionArgs = new ConditionArgs(token, context, this.ParentContext)
                : this.conditionArgs.Reuse(token.Name, token.Attributes, context);
        }

        public ConditionArgs ConditionArgs(string name, TokenAttributeDictionary attributes, object? context)
        {
            return this.conditionArgs == null
                ? this.conditionArgs = new ConditionArgs(name, attributes, context, this.ParentContext)
                : this.conditionArgs.Reuse(name, attributes, context);
        }

        public StringBuilder RentBuilder()
        {
            if (this.buildersInUse == this.builders.Length)
            {
                Array.Resize(ref this.builders, this.builders.Length + 2);
            }

            var builder = this.builders[this.buildersInUse] ??= new StringBuilder();
            this.buildersInUse++;
            return builder;
        }

        public void ReturnBuilder(StringBuilder builder)
        {
            builder.Clear();
            this.buildersInUse--;
        }

        // Removes up to count chars, but only text that this call wrote.
        public void RemovePrevious(StringBuilder builder, int count)
        {
            var written = builder.Length - this.StartOf(builder);
            if (written < count) count = written;
            builder.Remove(builder.Length - count, count);
        }

        // Removes one line break (\n or \r\n) at the end, but only text that this call wrote.
        public void RemovePreviousNewLine(StringBuilder builder)
        {
            int start = this.StartOf(builder);
            if (builder.Length <= start) return;

            if (builder[builder.Length - 1] == '\n')
                builder.Remove(builder.Length - 1, 1);

            if (builder.Length > start && builder[builder.Length - 1] == '\r')
                builder.Remove(builder.Length - 1, 1);
        }

        // The parent context is a stack, so the entry a block added is the last one equal to it.
        public void RemoveParent(object? value)
        {
            var parentContext = this.ParentContext;

            // Normally the entry is the last one. Checking the reference first skips Equals, which a record type runs over all its fields.
            int last = parentContext.Count - 1;
            if (last >= 0 && ReferenceEquals(parentContext[last], value))
            {
                parentContext.RemoveAt(last);
                return;
            }

            int index = parentContext.LastIndexOf(value);
            if (index >= 0)
            {
                parentContext.RemoveAt(index);
            }
        }

        // Copies text and writes the indent at the start of every line after the first, except before an empty line and after the last line break,
        // so no line gets trailing spaces.
        public static void AppendIndented(StringBuilder output, StringBuilder text, string indent)
        {
            bool lineStart = false;
            foreach (var chunk in text.GetChunks())
            {
                var span = chunk.Span;
                while (!span.IsEmpty)
                {
                    if (lineStart)
                    {
                        lineStart = false;
                        if (span[0] != '\n' && span[0] != '\r')
                        {
                            output.Append(indent);
                        }
                    }

                    int newLine = span.IndexOf('\n');
                    if (newLine < 0)
                    {
                        output.Append(span);
                        break;
                    }

                    output.Append(span.Slice(0, newLine + 1));
                    span = span.Slice(newLine + 1);
                    lineStart = true;
                }
            }
        }
    }
}
