// Title: Reuse a loop layout: REUSE_FOREACH
// Summary: Write a FOREACH layout once and run it again for another list. The loop value provider gets the new name and the attributes of the original FOREACH.

using Toshal.Template.Examples.Support;

namespace Toshal.Template.Examples;

public static class ReuseForEachExample
{
    public static void Run()
    {
        Console.WriteLine("== REUSE_FOREACH ==");

        const string template =
            "Open:\n<%FOREACH open style=\"bullet\"%>  * <%CONTEXT_AS_STRING%>\n<%NORECORD%>  (none)\n<%ENDNORECORD%><%ENDFOR%>" +
            "Done:\n<%REUSE_FOREACH open done%>";
        var tokens = new Parser().Parse(template);

        var lists = new Dictionary<string, string[]>
        {
            ["open"] = ["Write docs", "Fix bug"],
            ["done"] = [],
        };

        var seenNames = new List<string>();
        var processor = new Processor
        {
            LoopValueProvider = args =>
            {
                // args.Name is "open" for the FOREACH and "done" for the REUSE_FOREACH. The attributes come from the FOREACH.
                seenNames.Add($"{args.Name}/{args.Attributes.GetValue("style", "")}");
                return lists[args.Name];
            },
        };

        string text = processor.Process(new ProcessorArgs(tokens)).ToString();
        Console.Write(text);
        Verify.Equal("Open:\n  * Write docs\n  * Fix bug\nDone:\n  (none)\n", text, "reuse");
        Verify.That(string.Join(",", seenNames) == "open/bullet,done/bullet", "names and attributes");
    }
}
