// Title: Values and attributes
// Summary: A value tag can carry attributes such as format="0.00". The provider reads them with TokenArgs.GetAttribute; tokens and TokenAttributeDictionary expose them too.

using System.Globalization;
using Toshal.Template.Examples.Support;
using Toshal.Template.Tokens;

namespace Toshal.Template.Examples;

public static class ValuesAndAttributesExample
{
    public static void Run()
    {
        Console.WriteLine("== Values and attributes ==");

        const string template = "Total: <%=Total format=\"0.00\" currency=\"INR\"%> | Date: <%=Today format=\"dd MMM yyyy\"%> | Plain: <%=Total%>";
        var tokens = new Parser().Parse(template);

        var processor = new Processor
        {
            TokenValueProvider = args =>
            {
                // Attribute names are not case sensitive in GetAttribute. Values keep their case.
                string format = args.GetAttribute("FORMAT", "");
                return args.Name switch
                {
                    "total" => 1234.5m.ToString(format, CultureInfo.InvariantCulture) + (args.Attributes.ContainsKey("currency") ? " " + args.Attributes["currency"] : ""),
                    "today" => new DateTime(2026, 1, 15).ToString(format, CultureInfo.InvariantCulture),
                    _ => null,
                };
            },
        };

        string text = processor.Process(new ProcessorArgs(tokens)).ToString();
        Console.WriteLine(text);
        Verify.Equal("Total: 1234.50 INR | Date: 15 Jan 2026 | Plain: 1234.5", text, "attributes");

        // The same attributes on the parsed token.
        var totalToken = (NamedToken)tokens[1];
        Console.WriteLine($"Token '{totalToken.Name}': format={totalToken.GetAttribute("format", "?")}, missing={totalToken.GetAttribute("missing", "default")}");
        Verify.That(totalToken.Attributes.GetValue("Currency", "") == "INR", "TokenAttributeDictionary.GetValue");

        // Warning: the dictionary itself is case sensitive with lower case keys.
        Verify.That(totalToken.Attributes.ContainsKey("format") && !totalToken.Attributes.ContainsKey("Format"), "keys are lower case");
    }
}
