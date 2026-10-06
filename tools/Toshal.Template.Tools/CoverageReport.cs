using System.Globalization;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace Toshal.Template.Tools;

/// <summary>Turns the Cobertura file written by coverlet into a text table and a small HTML page.</summary>
internal static class CoverageReport
{
    private record Row(string Name, int Lines, int Covered)
    {
        public double Percent => Lines == 0 ? 100 : 100.0 * Covered / Lines;
    }

    public static int Run(string root, double minimum)
    {
        string dir = Path.Combine(root, "coverage");
        // One file for each test project: the lines of all of them are merged.
        var files = Directory.Exists(dir) ? Directory.GetFiles(dir, "coverage.cobertura.xml", SearchOption.AllDirectories) : Array.Empty<string>();
        if (files.Length == 0)
        {
            Console.Error.WriteLine("No coverage.cobertura.xml found in coverage/. Run the tests with coverage first.");
            return 1;
        }
        var hitLines = new Dictionary<string, Dictionary<(string File, int Line), bool>>();
        // Each report names its files relative to its own <source> folder, so make them full paths before the reports are merged.
        var classes = files.Select(f => XDocument.Load(f)).SelectMany(d =>
        {
            string source = d.Descendants("source").Select(e => e.Value).FirstOrDefault() ?? "";
            return d.Descendants("class").Select(c => (Class: c, Source: source));
        });
        foreach (var (c, source) in classes)
        {
            string name = (string)c.Attribute("name")!;
            int cut = name.IndexOf('/');
            if (cut >= 0) name = name[..cut];
            string fileName = Path.GetFullPath(Path.Combine(source, (string?)c.Attribute("filename") ?? ""));
            if (!hitLines.TryGetValue(name, out var map)) hitLines[name] = map = new();
            foreach (var l in c.Elements("lines").Elements("line"))
            {
                int number = int.Parse((string)l.Attribute("number")!, CultureInfo.InvariantCulture);
                bool hit = int.Parse((string)l.Attribute("hits")!, CultureInfo.InvariantCulture) > 0;
                map[(fileName, number)] = map.TryGetValue((fileName, number), out bool old) ? old || hit : hit;
            }
        }

        var rows = hitLines
            .Select(kv => new Row(kv.Key, kv.Value.Count, kv.Value.Values.Count(h => h)))
            .Where(r => r.Lines > 0)
            .OrderBy(r => r.Name)
            .ToList();
        var total = new Row("Total", rows.Sum(r => r.Lines), rows.Sum(r => r.Covered));

        var text = new StringBuilder();
        int width = Math.Max(5, rows.Max(r => r.Name.Length));
        text.AppendLine($"{"Class".PadRight(width)}  {"Lines",5}  {"Covered",7}  {"Line %",7}");
        foreach (var r in rows.Append(total))
        {
            text.AppendLine($"{r.Name.PadRight(width)}  {r.Lines,5}  {r.Covered,7}  {r.Percent,6:0.0}%");
        }
        Console.Write(text);
        File.WriteAllText(Path.Combine(dir, "summary.txt"), text.ToString());
        WriteHtml(Path.Combine(dir, "index.html"), rows, total);
        Console.WriteLine($"HTML report: {Path.Combine(dir, "index.html")}");

        if (total.Percent < minimum)
        {
            Console.Error.WriteLine($"Line coverage {total.Percent:0.0}% is below {minimum:0.#}%.");
            return 1;
        }
        return 0;
    }

    private static void WriteHtml(string path, List<Row> rows, Row total)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>Line coverage</title>");
        sb.AppendLine("<style>body{font-family:system-ui,sans-serif;margin:2rem auto;max-width:52rem;padding:0 1rem}table{border-collapse:collapse;width:100%}th,td{padding:.4rem .6rem;border-bottom:1px solid #8884;text-align:left}td.n,th.n{text-align:right}.bar{background:#8883;height:.6rem;border-radius:.3rem;overflow:hidden;min-width:6rem}.bar i{display:block;height:100%;background:#2a9d4a}tr.total td{font-weight:600}</style></head><body>");
        sb.AppendLine("<h1>Line coverage</h1><p>Settings are in <code>coverlet.runsettings</code>. No class is excluded.</p>");
        sb.AppendLine("<table><tr><th>Class</th><th class=\"n\">Lines</th><th class=\"n\">Covered</th><th class=\"n\">Line %</th><th></th></tr>");
        foreach (var r in rows.Append(total))
        {
            string cls = r.Name == "Total" ? " class=\"total\"" : "";
            sb.AppendLine($"<tr{cls}><td>{WebUtility.HtmlEncode(r.Name)}</td><td class=\"n\">{r.Lines}</td><td class=\"n\">{r.Covered}</td><td class=\"n\">{r.Percent:0.0}%</td><td><div class=\"bar\"><i style=\"width:{r.Percent.ToString("0.#", CultureInfo.InvariantCulture)}%\"></i></div></td></tr>");
        }
        sb.AppendLine("</table></body></html>");
        File.WriteAllText(path, sb.ToString());
    }
}
