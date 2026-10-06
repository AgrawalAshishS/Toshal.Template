// Title: Whitespace control
// Summary: Put control tags on their own lines and indent them like code: a line that holds only control tags writes nothing, not even its line break. REMOVE_PREVIOUS n still removes a last separator.

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

        // Remove the last separator of a list.
        const string csv = "<%FOREACH items%><%CONTEXT_AS_STRING%>, <%ENDFOR%><%REMOVE_PREVIOUS 2%>.";
        string line = processor.Process(new ProcessorArgs(new Parser().Parse(csv))).ToString();
        Console.WriteLine(line);
        Verify.Equal("Red, Green.", line, "remove previous 2");
    }
}
