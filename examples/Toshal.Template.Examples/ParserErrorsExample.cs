// Title: Parser errors
// Summary: A broken template fails in Parser.Parse with a ParserException (or a more specific subclass) that tells the line and column. Show it to the template author.

using Toshal.Template.Examples.Support;
using Toshal.Template.Exceptions;

namespace Toshal.Template.Examples;

public static class ParserErrorsExample
{
    public static void Run()
    {
        Console.WriteLine("== Parser errors ==");

        var broken = new (string Template, Type Expected)[]
        {
            ("Hello\n<%IF vip%>Dear", typeof(TokenNotClosedException)),
            ("<%=%>", typeof(TokenMissingNameException)),
            ("<%=Total format=0.00%>", typeof(InvalidTokenAttributeException)),
            ("<%REUSE_FOREACH lines copy%>", typeof(ForEachMissingForReuseException)),
            ("<%UNKNOWN%>", typeof(ParserException)),
            ("<%REMOVE_PREVIOUS x%>", typeof(ParserException)),
        };

        foreach (var (template, expected) in broken)
        {
            try
            {
                new Parser().Parse(template);
                Verify.That(false, "should have failed: " + template);
            }
            catch (ParserException ex)
            {
                // Split is the failing piece; it is null for ForEachMissingForReuseException.
                Console.WriteLine($"{ex.GetType().Name,-34} line {ex.LineNumber}, column {ex.StartingPosition}: {ex.Message} [{ex.Split?.Content}]");
                Verify.That(ex.GetType() == expected, "exception type for " + template);
            }
        }

        // The specific exceptions carry more details.
        try { new Parser().Parse("Hello\n<%IF vip%>Dear"); }
        catch (TokenNotClosedException ex) { Verify.That(ex.TokenName == "vip" && ex.LineNumber == 2 && ex.StartingPosition == 1, "TokenNotClosedException details"); }

        try { new Parser().Parse("<%REUSE_FOREACH lines copy%>"); }
        catch (ForEachMissingForReuseException ex) { Verify.That(ex.ForEachName == "lines" && ex.ReuseName == "copy" && ex.Split == null, "ForEachMissingForReuseException details"); }

        // The exceptions have public constructors, so your own template checks can report errors the same way.
        var split = new Split { Content = "<%=Secret%>", LineNumber = 3, StartingPosition = 5 };
        var errors = new List<ParserException>
        {
            new ParserException(split),
            new ParserException(split, "Secret values are not allowed"),
            new ParserException(3, 5, "Line too long"),
            new TokenMissingNameException(split),
            new InvalidTokenAttributeException(split),
            new TokenNotClosedException(split, "secret"),
            new TokenNotClosedException(split, "secret", "Close it"),
            new ForEachMissingForReuseException("copy", "lines", 3, 5),
        };
        Verify.That(errors.All(e => e.LineNumber == 3 && e.StartingPosition == 5), "positions");
        Console.WriteLine(errors[0].Message);
    }
}
