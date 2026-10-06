// Copyright (c) 2026 Toshal Infotech. Licensed under the MIT License. See LICENSE in the repository root.

namespace Toshal.Template.CodeGen
{
    using System.Globalization;
    using System.Text;

    // Writes C# lines with four space indents and CRLF line breaks, the style of this repository.
    internal sealed class CodeWriter
    {
        private readonly StringBuilder text = new StringBuilder();
        private int depth;

        public CodeWriter(int depth = 0)
        {
            this.depth = depth;
        }

        public void Line(string line = "")
        {
            if (line.Length > 0)
            {
                this.text.Append(' ', this.depth * 4).Append(line);
            }

            this.text.Append("\r\n");
        }

        public void Open(string? line = null)
        {
            if (line != null) this.Line(line);
            this.Line("{");
            this.depth++;
        }

        public void Close(string closing = "}")
        {
            this.depth--;
            this.Line(closing);
        }

        public override string ToString() => this.text.ToString();

        // A C# string literal with the same text.
        public static string Literal(string value)
        {
            var sb = new StringBuilder(value.Length + 2);
            sb.Append('"');
            foreach (var c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\0': sb.Append("\\0"); break;
                    default:
                        if (char.IsControl(c) || c == (char)0x2028 || c == (char)0x2029)
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(c);
                        }

                        break;
                }
            }

            sb.Append('"');
            return sb.ToString();
        }
    }
}
