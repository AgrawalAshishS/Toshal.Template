// Title: Whitespace control
// Summary: Put control tags on their own lines and indent them like code: a line that holds only control tags writes nothing. SEPARATOR writes a separator between rows, and a sub template on an indented line is indented as a whole.

using Toshal.Template.Examples.Support;

namespace Toshal.Template.Examples;

public static class WhitespaceExample
{
    public static void Run()
    {
        Console.WriteLine("== Whitespace ==");

        // The FOREACH and ENDFOR lines hold only control tags, so they write nothing: no indent, no line break.
        // The lines with text keep their indent and their line break.
        const string template =
            "public enum Color\n" +
            "{\n" +
            "    <%FOREACH items%>\n" +
            "    <%CONTEXT_AS_STRING%>,\n" +
            "    <%ENDFOR%>\n" +
            "}\n";
        var processor = new Processor { LoopValueProvider = args => new[] { "Red", "Green" } };
        string text = processor.Process(new ProcessorArgs(new Parser().Parse(template))).ToString();
        Console.WriteLine(text);
        Verify.Equal("public enum Color\n{\n    Red,\n    Green,\n}\n", text, "tag lines");

        // A line with text or a value keeps everything. Here the IF is inside a text line, so only the tags go.
        const string inline = "Status: <%IF items%>has items<%ENDIF%>\n";
        var inlineProcessor = new Processor { ConditionValueProvider = args => true };
        string status = inlineProcessor.Process(new ProcessorArgs(new Parser().Parse(inline))).ToString();
        Console.WriteLine(status);
        Verify.Equal("Status: has items\n", status, "tags inside a text line");

        // SEPARATOR is written in every row except the last: no trailing comma, and no characters to count.
        const string parameters =
            "public Paint(\n" +
            "    <%FOREACH items%>\n" +
            "    Color <%CONTEXT_AS_STRING%><%SEPARATOR%>,<%ENDSEPARATOR%>\n" +
            "    <%ENDFOR%>\n" +
            ")\n";
        string signature = processor.Process(new ProcessorArgs(new Parser().Parse(parameters))).ToString();
        Console.WriteLine(signature);
        Verify.Equal("public Paint(\n    Color Red,\n    Color Green\n)\n", signature, "separator");

        // A sub template alone on an indented line gets that indent on every line; its own indent stays.
        var nullCheck = new Parser().Parse("if (color == null)\n{\n    throw new ArgumentNullException(nameof(color));\n}");
        var subProcessor = new Processor { ProcessTemplateValueProvider = args => nullCheck };
        const string method = "    public void Paint(Color color)\n    {\n        <%PROCESS_TEMPLATE null_check%>\n    }\n";
        string body = subProcessor.Process(new ProcessorArgs(new Parser().Parse(method))).ToString();
        Console.WriteLine(body);
        Verify.Equal(
            "    public void Paint(Color color)\n    {\n        if (color == null)\n        {\n            throw new ArgumentNullException(nameof(color));\n        }\n    }\n",
            body,
            "indented sub template");
    }
}
