// Title: Realistic: a welcome email
// Summary: A welcome email from an embedded template file, filled from plain C# objects by the property reading providers. It uses IF/ELSE, FOREACH, a formatted date and a shared signature sub template.

using Toshal.Template.Examples.Support;

namespace Toshal.Template.Examples;

public static class EmailExample
{
    public record Member(string Name, string Email);

    public record Signup(string FirstName, string Product, string Plan, bool IsTrial, DateTime TrialEnds, List<Member> Team)
    {
        public bool HasTeam => Team.Count > 0;

        public int TeamCount => Team.Count;
    }

    /// <summary>Production code: one method that turns a signup into the email text.</summary>
    public static string Render(Signup signup)
    {
        // The signature is a separate template file, shared by all emails.
        var processor = ObjectProviders.Create(subTemplates: args => EmbeddedTemplates.Tokens("Signature.txt"));
        return processor.Process(new ProcessorArgs(EmbeddedTemplates.Tokens("WelcomeEmail.txt")) { Context = signup }).ToString();
    }

    public static void Run()
    {
        Console.WriteLine("== Welcome email ==");

        var withTeam = new Signup("Asha", "Toshal Cloud", "Pro", true, new DateTime(2026, 2, 15),
            [new Member("Ravi", "ravi@example.com"), new Member("Mira", "mira@example.com")]);
        string email = Render(withTeam);
        Console.WriteLine(email);

        Verify.Equal(
            "Subject: Welcome to Toshal Cloud, Asha!\n" +
            "\n" +
            "Hi Asha,\n" +
            "\n" +
            "Thanks for joining Toshal Cloud on the Pro plan.\n" +
            "Your free trial ends on 15 Feb 2026.\n" +
            "\n" +
            "You invited 2 people:\n" +
            "  - Ravi <ravi@example.com>\n" +
            "  - Mira <mira@example.com>\n" +
            "\n" +
            "--\n" +
            "The Toshal Cloud team",
            email,
            "email with a team");

        string alone = Render(new Signup("Ravi", "Toshal Cloud", "Basic", false, default, []));
        Verify.That(alone.Contains("on the Basic plan.\n\nInvite your team from Settings > Team.") && !alone.Contains("trial"), "email without a team");
    }
}
