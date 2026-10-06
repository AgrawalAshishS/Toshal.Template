// Title: Loops: FOREACH and its parts
// Summary: The loop value provider returns a list. HEADER and FOOTER run once, ROW runs per item, ALTROW on every second row, FIRSTROW and LASTROW at the ends, and NORECORD when the list is empty.

using Toshal.Template.Examples.Support;

namespace Toshal.Template.Examples;

public static class LoopsExample
{
    public static void Run()
    {
        Console.WriteLine("== Loops ==");

        const string template =
            "<%FOREACH Fruits top=\"3\"%>" +
            "<%HEADER%>[<%ENDHEADER%>" +
            "<%FIRSTROW%><%CONTEXT_AS_STRING%><%ENDFIRSTROW%>" +
            "<%ROW%>, <%CONTEXT_AS_STRING%><%ENDROW%>" +
            "<%ALTROW%>; <%CONTEXT_AS_STRING%><%ENDALTROW%>" +
            "<%LASTROW%> and <%CONTEXT_AS_STRING%><%ENDLASTROW%>" +
            "<%FOOTER%>] (<%=Count%>)<%ENDFOOTER%>" +
            "<%NORECORD%>(no fruit)<%ENDNORECORD%>" +
            "<%ENDFOR%>";
        var tokens = new Parser().Parse(template);

        List<string> fruits = [];
        var processor = new Processor
        {
            // LoopArgs carries the loop name and the attributes of the FOREACH tag.
            LoopValueProvider = args => fruits.Take(int.Parse(args.Attributes.GetValue("top", "100"))).ToList(),
            // Inside HEADER and FOOTER the context is the list itself.
            TokenValueProvider = args => args.Name == "count" && args.Context is List<string> list ? list.Count.ToString() : null,
        };

        string Render() => processor.Process(new ProcessorArgs(tokens)).ToString();

        fruits = ["apple", "banana", "cherry", "date"];
        Console.WriteLine(Render());
        Verify.Equal("[apple; banana and cherry] (3)", Render(), "first, alt and last rows");

        fruits = ["apple", "banana"];
        Verify.Equal("[apple and banana] (2)", Render(), "two rows");

        fruits = [];
        Console.WriteLine(Render());
        Verify.Equal("(no fruit)", Render(), "no record");

        // Before and after parts wrap each row. Here they build an HTML list.
        var html = new Parser().Parse("<ul><%FOREACH items%><%BEFOREROW%><li><%ENDBEFOREROW%><%CONTEXT_AS_STRING%><%AFTERROW%></li><%ENDAFTERROW%><%ENDFOR%></ul>");
        string list = new Processor { LoopValueProvider = args => new[] { "one", "two" } }.Process(new ProcessorArgs(html)).ToString();
        Console.WriteLine(list);
        Verify.Equal("<ul><li>one</li><li>two</li></ul>", list, "before and after row");
    }
}
