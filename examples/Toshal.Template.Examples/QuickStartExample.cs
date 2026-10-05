// Title: Quick start
// Summary: Parse a template once, set a provider, and process it. Parser.Parse, ProcessorArgs and Processor.Process are all you need for the first template.

using Toshal.Template.Examples.Support;
using Toshal.Template.Tokens;

namespace Toshal.Template.Examples;

public static class QuickStartExample
{
    public static void Run()
    {
        Console.WriteLine("== Quick start ==");

        // 1. Parse. Do this once per template; the tokens can be processed many times.
        List<IToken> tokens = new Parser().Parse("Hello <%=Name%>, you have <%=Count%> new messages.");

        // 2. Say what each name means. Names arrive in lower case.
        var processor = new Processor
        {
            TokenValueProvider = args => args.Name switch
            {
                "name" => "Asha",
                "count" => "3",
                _ => null,   // null writes nothing
            },
        };

        // 3. Process.
        string text = processor.Process(new ProcessorArgs(tokens)).ToString();

        Console.WriteLine(text);
        Verify.Equal("Hello Asha, you have 3 new messages.", text, "quick start");

        // The same tokens with a context object: providers read it from args.Context.
        var people = new[] { ("Ravi", 1), ("Mira", 0) };
        var fromContext = new Processor
        {
            TokenValueProvider = args =>
            {
                var (name, count) = ((string, int))args.Context!;
                return args.Name == "name" ? name : count.ToString();
            },
        };

        foreach (var person in people)
        {
            Console.WriteLine(fromContext.Process(new ProcessorArgs(tokens) { Context = person }));
        }

        Verify.Equal("Hello Mira, you have 0 new messages.", fromContext.Process(new ProcessorArgs(tokens) { Context = people[1] }).ToString(), "context");
    }
}
