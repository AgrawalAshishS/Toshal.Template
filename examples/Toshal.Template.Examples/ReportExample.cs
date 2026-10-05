// Title: Realistic: a text report
// Summary: A monthly sales report as an aligned text table, with a header, a footer with totals and a "no data" text. The footer reads the totals from the report object through the parent contexts.

using System.Globalization;
using Toshal.Template.Examples.Support;

namespace Toshal.Template.Examples;

public static class ReportExample
{
    public record Region(string Name, int Orders, decimal Amount);

    public record SalesReport(DateTime Month, List<Region> Regions)
    {
        public int TotalOrders => Regions.Sum(r => r.Orders);

        public decimal TotalAmount => Regions.Sum(r => r.Amount);

        public string? Best => Regions.OrderByDescending(r => r.Amount).FirstOrDefault()?.Name;
    }

    public static string Render(SalesReport report)
    {
        return ObjectProviders.Create().Process(new ProcessorArgs(EmbeddedTemplates.Tokens("SalesReport.txt")) { Context = report }).ToString();
    }

    public static void Run()
    {
        Console.WriteLine("== Sales report ==");

        var march = new SalesReport(new DateTime(2026, 3, 1), [new Region("North", 12, 1250m), new Region("South", 3, 310.5m)]);
        string text = Render(march);
        Console.WriteLine(text);

        string Line(string name, int orders, decimal amount) => string.Format(CultureInfo.InvariantCulture, "{0,-11}  {1,6}  {2,11:N2}", name, orders, amount);
        Verify.Equal(
            "Sales report for March 2026\n" +
            "\n" +
            "Region       Orders       Amount\n" +
            "-----------  ------  -----------\n" +
            Line("North", 12, 1250m) + "\n" +
            Line("South", 3, 310.5m) + "\n" +
            "-----------  ------  -----------\n" +
            string.Format(CultureInfo.InvariantCulture, "Total        {0,6}  {1,11:N2}", 15, 1560.5m) + "\n" +
            "\n" +
            "Best region: North",
            text,
            "report");

        string empty = Render(new SalesReport(new DateTime(2026, 4, 1), []));
        Console.WriteLine(empty);
        Verify.Equal("Sales report for April 2026\n\nNo sales this month.\n", empty, "empty report");
    }
}
