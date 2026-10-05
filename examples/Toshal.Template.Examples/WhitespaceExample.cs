// Title: Whitespace control
// Summary: Keep the template readable with one tag per line, and remove the extra line breaks and separators from the output with REMOVE_PREVIOUS_NEW_LINE and REMOVE_PREVIOUS n.

using Toshal.Template.Examples.Support;

namespace Toshal.Template.Examples;

public static class WhitespaceExample
{
    public static void Run()
    {
        Console.WriteLine("== Whitespace ==");

        // Without control, every tag line leaves a line break behind.
        const string template =
            "Items:\n" +
            "<%FOREACH items%>\n<%REMOVE_PREVIOUS_NEW_LINE%>" +
            "- <%CONTEXT_AS_STRING%>\n" +
            "<%ENDFOR%>\n<%REMOVE_PREVIOUS_NEW_LINE%>" +
            "End";
        var processor = new Processor { LoopValueProvider = args => new[] { "one", "two" } };
        string text = processor.Process(new ProcessorArgs(new Parser().Parse(template))).ToString();
        Console.WriteLine(text);
        Verify.Equal("Items:\n- one\n- two\nEnd", text, "remove previous new line");

        // Remove the last separator of a list.
        const string csv = "<%FOREACH items%><%CONTEXT_AS_STRING%>, <%ENDFOR%><%REMOVE_PREVIOUS 2%>.";
        string line = processor.Process(new ProcessorArgs(new Parser().Parse(csv))).ToString();
        Console.WriteLine(line);
        Verify.Equal("one, two.", line, "remove previous 2");
    }
}
