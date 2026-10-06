// Title: Conditions: IF, ELSEIF, ELSE and not
// Summary: The condition value provider answers each named condition with true or false. The processor handles "not", ELSEIF chains and ELSE.

using Toshal.Template.Examples.Support;

namespace Toshal.Template.Examples;

public static class ConditionsExample
{
    private record Account(decimal Balance, bool Locked);

    public static void Run()
    {
        Console.WriteLine("== Conditions ==");

        const string template =
            "<%IF Locked THEN%>Your account is locked." +
            "<%ELSEIF Negative%>You owe money." +
            "<%ELSEIF Low limit=\"100\"%>Your balance is low." +
            "<%ELSE%>All good.<%ENDIF%>" +
            "<%IF not Locked%> You can log in.<%ENDIF%>";
        var tokens = new Parser().Parse(template);

        var processor = new Processor
        {
            ConditionValueProvider = args =>
            {
                var account = (Account)args.Context!;
                return args.Name switch
                {
                    "locked" => account.Locked,
                    "negative" => account.Balance < 0,
                    // Attributes work on conditions too. Values keep their case; LowerCaseValues has a lower case copy.
                    "low" => account.Balance < decimal.Parse(args.Attributes.GetValue("limit", "0")),
                    _ => false,
                };
            },
        };

        string Render(Account a) => processor.Process(new ProcessorArgs(tokens) { Context = a }).ToString();

        foreach (var account in new[] { new Account(10, true), new Account(-5, false), new Account(50, false), new Account(500, false) })
        {
            Console.WriteLine($"{account} -> {Render(account)}");
        }

        Verify.Equal("Your account is locked.", Render(new Account(10, true)), "locked");
        Verify.Equal("You owe money. You can log in.", Render(new Account(-5, false)), "negative");
        Verify.Equal("Your balance is low. You can log in.", Render(new Account(50, false)), "low");
        Verify.Equal("All good. You can log in.", Render(new Account(500, false)), "else");
    }
}
