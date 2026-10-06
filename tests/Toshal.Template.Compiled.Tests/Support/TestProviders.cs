using System.Collections;
using System.Text;

using Toshal.Template.Compiled;
using Toshal.Template.Tokens;

namespace Toshal.Template.Compiled.Tests.Support
{
    // Answers for any template, so the same template can run through Processor and through its compiled class.
    // A context that is a dictionary answers by key; for other names the answer is made from the args, so every detail of the args
    // (name, attributes, lower case copy, context, parent stack) shows up in the text and a difference fails the test.
    public sealed class TestProviders
    {
        // Rows of a FOREACH that the context does not answer.
        public int RowCount { get; set; } = 3;

        // Sub templates for PROCESS_TEMPLATE, by lower case name.
        public Dictionary<string, string> SubTemplates { get; } = new Dictionary<string, string>
        {
            ["footer"] = "F <%=name%>\r\n<%SET x%>s<%ENDSET%>end <%=x%>\r\n  last<%=outer%>",
        };

        // Set by the harness: makes the compiled class of a sub template, with these providers.
        public Func<string, CompiledTemplate?>? CompiledSubTemplate { get; set; }

        public string? Token(TokenArgs args)
        {
            if (args.Context is IDictionary<string, object?> map && map.TryGetValue(args.Name, out var value))
            {
                return value?.ToString();
            }

            if (args.Name == "empty") return string.Empty;
            if (args.Name == "null") return null;

            var text = new StringBuilder();
            text.Append('{').Append(args.Name);
            foreach (var pair in args.Attributes.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                text.Append(' ').Append(pair.Key).Append('=').Append(pair.Value).Append('/').Append(args.Attributes.LowerCaseValues[pair.Key]);
            }

            text.Append(" @").Append(args.ParentContext.Count).Append(' ').Append(Describe(args.Context)).Append('}');
            return text.ToString();
        }

        public void Write(TokenArgs args, StringBuilder output) => output.Append(this.Token(args));

        public bool Condition(ConditionArgs args)
        {
            if (args.Context is IDictionary<string, object?> map && map.TryGetValue(args.Name, out var value) && value is bool b)
            {
                return b;
            }

            return (args.Name.Length + args.ParentContext.Count + args.Attributes.Count) % 2 == 0;
        }

        public IList? Loop(LoopArgs args)
        {
            if (args.Context is IDictionary<string, object?> map && map.TryGetValue(args.Name, out var value))
            {
                return value as IList;
            }

            if (args.ParentContext.Count > 6) return null;

            var rows = new List<object?>();
            for (int i = 0; i < this.RowCount; i++)
            {
                rows.Add(new Dictionary<string, object?> { ["name"] = args.Name + i });
            }

            return rows;
        }

        public object? With(TokenArgs args)
        {
            if (args.Context is IDictionary<string, object?> map && map.TryGetValue(args.Name, out var value))
            {
                return value;
            }

            return args.Name.Contains("missing", StringComparison.Ordinal) ? null : new Dictionary<string, object?> { ["name"] = "w:" + args.Name };
        }

        public List<IToken>? ParsedSubTemplate(ProcessTemplateArgs args) =>
            this.SubTemplates.TryGetValue(args.Name, out var text) ? new Parser().Parse(text) : null;

        public CompiledTemplate? CompiledSub(ProcessTemplateArgs args) =>
            this.SubTemplates.ContainsKey(args.Name) ? this.CompiledSubTemplate?.Invoke(args.Name) : null;

        private static string Describe(object? context) => context switch
        {
            null => "null",
            IDictionary<string, object?> map => map.TryGetValue("name", out var name) ? "ctx:" + name : "ctx",
            IList list => "list" + list.Count,
            _ => context.ToString() ?? string.Empty,
        };
    }
}
