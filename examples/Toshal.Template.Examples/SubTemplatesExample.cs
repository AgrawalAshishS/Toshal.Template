// Title: Sub templates: PROCESS_TEMPLATE
// Summary: Share a header or footer between templates. The process template provider returns the parsed tokens of the named part; they run in place with the current context.

using Toshal.Template.Examples.Support;
using Toshal.Template.Tokens;

namespace Toshal.Template.Examples;

public static class SubTemplatesExample
{
    public static void Run()
    {
        Console.WriteLine("== PROCESS_TEMPLATE ==");

        var parser = new Parser();
        var parts = new Dictionary<string, List<IToken>>
        {
            ["signature"] = parser.Parse("-- \n<%=Sender%>, Toshal Infotech"),
            ["signature_hi"] = parser.Parse("-- \n<%=Sender%>, Toshal Infotech (dhanyavaad)"),
        };

        var processor = new Processor
        {
            TokenValueProvider = args => args.Name == "sender" ? "Ravi" : null,
            // The lang attribute picks the language of the part. ProcessTemplateArgs.GetAttribute reads it.
            ProcessTemplateValueProvider = args =>
            {
                string lang = args.GetAttribute("lang", "en");
                return parts.GetValueOrDefault(lang == "en" ? args.Name : args.Name + "_" + lang);
            },
        };

        string english = processor.Process(new ProcessorArgs(parser.Parse("Thanks!\n<%PROCESS_TEMPLATE Signature%>"))).ToString();
        string hindi = processor.Process(new ProcessorArgs(parser.Parse("Thanks!\n<%PROCESS_TEMPLATE Signature lang=\"hi\"%>"))).ToString();
        Console.WriteLine(english);
        Verify.Equal("Thanks!\n-- \nRavi, Toshal Infotech", english, "sub template");
        Verify.Equal("Thanks!\n-- \nRavi, Toshal Infotech (dhanyavaad)", hindi, "attribute");

        // An unknown part (provider returns null) writes nothing.
        string none = processor.Process(new ProcessorArgs(parser.Parse("[<%PROCESS_TEMPLATE missing%>]"))).ToString();
        Verify.Equal("[]", none, "null writes nothing");
    }
}
