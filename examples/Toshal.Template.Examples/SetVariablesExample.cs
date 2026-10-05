// Title: Variables: SET
// Summary: SET writes text into a variable instead of the output. A later value tag with that name writes the variable. Variables follow the blocks: a SET inside WITH stays inside, a SET inside a ROW is shared by the rows of that loop.

using Toshal.Template.Examples.Support;

namespace Toshal.Template.Examples;

public static class SetVariablesExample
{
    public static void Run()
    {
        Console.WriteLine("== SET ==");

        // Build a value once and use it twice. The variable wins over the token provider.
        const string template = "<%SET greeting%>Dear <%=Name%><%ENDSET%><%=greeting%>,\n...\nThanks again, <%=greeting%>.";
        var processor = new Processor { TokenValueProvider = args => args.Name == "name" ? "Asha" : "(from provider)" };
        string text = processor.Process(new ProcessorArgs(new Parser().Parse(template))).ToString();
        Console.WriteLine(text);
        Verify.Equal("Dear Asha,\n...\nThanks again, Dear Asha.", text, "set");

        // A running separator: empty before the first row, ", " after it. The SET in ROW is seen by the next rows.
        const string joined = "<%FOREACH tags%><%=sep%><%CONTEXT_AS_STRING%><%SET sep%>, <%ENDSET%><%ENDFOR%>[<%=sep%>]";
        var loop = new Processor { LoopValueProvider = args => new[] { "a", "b", "c" } };
        string list = loop.Process(new ProcessorArgs(new Parser().Parse(joined))).ToString();
        Console.WriteLine(list);
        // After the loop the variable is gone again.
        Verify.Equal("a, b, c[]", list, "row level variable");
    }
}
