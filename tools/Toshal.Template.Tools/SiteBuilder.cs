using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Toshal.Template.Tools;

/// <summary>
/// Writes the website into docs/. Pages are built from: the XML comments (API), the example files and the template files (Examples),
/// docs/known-issues.md, CONTRIBUTING.md and the Markdown files in tools/Toshal.Template.Tools/content.
/// </summary>
internal static class SiteBuilder
{
    // Prefix for links to API pages. The home page is one folder above the api folder.
    private static string linkBase = "";

    private record Example(string File, string Slug, string Title, string Summary, string Code);

    // The generated files. known-issues.md is written by hand and is not in this list.
    private static readonly string[] GeneratedFolders = { "api", "examples", "assets" };
    private static readonly string[] GeneratedPages = { "index.html", "why.html", "getting-started.html", "syntax.html", "pattern.html", "known-issues.html", "contributing.html", ".nojekyll" };

    /// <summary>Builds the site in a temp folder and compares it with docs/. Returns 1 and lists the differences when docs/ is out of date.</summary>
    public static int Check(string root)
    {
        var differences = FindDifferences(root);
        if (differences.Count == 0)
        {
            Console.WriteLine("Docs are up to date.");
            return 0;
        }

        Console.Error.WriteLine("Docs are out of date. Run docs.cmd and commit the result. Different files:");
        foreach (var d in differences) Console.Error.WriteLine("  " + d);
        return 1;
    }

    /// <summary>Builds the site in a temp folder and lists the generated files that differ from docs/. An empty list means the docs are up to date.</summary>
    public static List<string> FindDifferences(string root)
    {
        string temp = Path.Combine(Path.GetTempPath(), "toshal-template-docs-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(temp);
            File.Copy(Path.Combine(root, "docs", "known-issues.md"), Path.Combine(temp, "known-issues.md"));
            Build(root, temp, quiet: true);

            return Differences(Path.Combine(root, "docs"), temp).ToList();
        }
        finally
        {
            if (Directory.Exists(temp)) Directory.Delete(temp, true);
        }
    }

    /// <summary>The generated files that are missing, extra or different between the two folders.</summary>
    public static IEnumerable<string> Differences(string docs, string fresh)
    {
        IEnumerable<string> Files(string dir) => GeneratedPages.Where(p => File.Exists(Path.Combine(dir, p)))
            .Concat(GeneratedFolders.Where(f => Directory.Exists(Path.Combine(dir, f)))
                .SelectMany(f => Directory.GetFiles(Path.Combine(dir, f), "*", SearchOption.AllDirectories).Select(x => Path.GetRelativePath(dir, x))))
            .Select(p => p.Replace('\\', '/'));

        var have = Files(docs).ToHashSet();
        var want = Files(fresh).ToHashSet();
        foreach (var f in want.Except(have).Order()) yield return f + " (missing)";
        foreach (var f in have.Except(want).Order()) yield return f + " (should not exist)";
        foreach (var f in want.Intersect(have).Order())
        {
            // Compare text without line ending differences, so a CRLF checkout does not count as a change.
            string a = File.ReadAllText(Path.Combine(docs, f)).Replace("\r\n", "\n");
            string b = File.ReadAllText(Path.Combine(fresh, f)).Replace("\r\n", "\n");
            if (a != b) yield return f + " (changed)";
        }
    }

    public static int Run(string root, string docs)
    {
        Build(root, docs, quiet: false);
        return 0;
    }

    private static void Build(string root, string docs, bool quiet)
    {
        var api = new ApiModel(new[] { typeof(Parser).Assembly });
        var examples = LoadExamples(root);

        // Remove pages made earlier, so a removed type or example does not stay on the site. Hand-written files are kept.
        foreach (var sub in GeneratedFolders)
        {
            string p = Path.Combine(docs, sub);
            if (Directory.Exists(p)) Directory.Delete(p, true);
        }
        foreach (var f in GeneratedPages)
        {
            File.Delete(Path.Combine(docs, f));
        }
        foreach (var sub in GeneratedFolders) Directory.CreateDirectory(Path.Combine(docs, sub));

        File.WriteAllText(Path.Combine(docs, "assets", "site.css"), Css);
        File.WriteAllText(Path.Combine(docs, ".nojekyll"), "");

        string content = Path.Combine(root, "tools", "Toshal.Template.Tools", "content");
        string Md(string file) => Markdown.ToHtml(File.ReadAllText(Path.Combine(content, file)), root);

        // Home lists the types, with links into api/.
        linkBase = "api/";
        Write(docs, "index.html", Page("Home", "", "index", Md("home.md") + "<h2>What is inside</h2>\n" + TypeTable(api, pageIsInApi: false)));

        linkBase = "";
        Write(docs, "why.html", Page("Why use it", "", "why", Md("why.md")));
        Write(docs, "getting-started.html", Page("Getting started", "", "getting-started", Md("getting-started.md")));
        Write(docs, "syntax.html", Page("Template syntax", "", "syntax", Md("syntax.md")));
        Write(docs, "pattern.html", Page("Testing pattern", "", "pattern", Md("pattern.md")));
        Write(docs, "known-issues.html", Page("Known issues", "", "known-issues", Markdown.ToHtml(File.ReadAllText(Path.Combine(docs, "known-issues.md")), root)));
        Write(docs, "contributing.html", Page("Contributing", "", "contributing", Markdown.ToHtml(File.ReadAllText(Path.Combine(root, "CONTRIBUTING.md")), root)));

        // API
        var apiIndex = new StringBuilder("<h1>API reference</h1>\n<p>Made from the XML comments of the library.</p>\n");
        apiIndex.Append(TypeTable(api, pageIsInApi: true));
        Write(docs, "api/index.html", Page("API reference", "../", "api", apiIndex.ToString()));
        foreach (var t in api.Types)
        {
            Write(docs, $"api/{t.FileName}.html", Page(t.Name, "../", "api", TypePage(api, t)));
        }

        // Examples, then one page with all template files.
        var exIndex = new StringBuilder("<h1>Examples</h1>\n<p>Each page is one file of the runnable project <code>examples/Toshal.Template.Examples</code>. Every example checks its own output, and the tests run them all. Run them with <code>dotnet run --project examples/Toshal.Template.Examples</code>.</p>\n<ul>\n");
        foreach (var e in examples) exIndex.AppendLine($"<li><a href=\"{e.Slug}.html\">{Enc(e.Title)}</a>: {Enc(e.Summary)}</li>");
        exIndex.AppendLine("<li><a href=\"templates.html\">Template files</a>: the .txt templates that the realistic examples embed.</li>");
        exIndex.AppendLine("</ul>");
        Write(docs, "examples/index.html", Page("Examples", "../", "examples", exIndex.ToString()));
        foreach (var e in examples)
        {
            string body = $"<h1>{Enc(e.Title)}</h1>\n<p>{Enc(e.Summary)}</p>\n<p class=\"muted\">File: <a href=\"{RepoRoot.RepoUrl}/blob/{RepoRoot.Branch}/{e.File}\">{Enc(e.File)}</a></p>\n<pre><code class=\"lang-csharp\">{Enc(e.Code)}</code></pre>";
            Write(docs, $"examples/{e.Slug}.html", Page(e.Title, "../", "examples", body));
        }
        Write(docs, "examples/templates.html", Page("Template files", "../", "examples", TemplatesPage(root)));

        if (!quiet) Console.WriteLine($"Site written to docs/: {api.Types.Count} API pages, {examples.Count} example pages.");
    }

    private static List<Example> LoadExamples(string root)
    {
        var list = new List<Example>();
        string examplesRoot = Path.Combine(root, "examples", "Toshal.Template.Examples");
        var files = Directory.GetFiles(examplesRoot, "*.cs")
            .Concat(Directory.GetFiles(Path.Combine(examplesRoot, "Patterns"), "*.cs"))
            .Concat(Directory.GetFiles(Path.Combine(examplesRoot, "Support"), "*.cs").Where(f => !f.EndsWith("Verify.cs")))
            .OrderBy(f => f, StringComparer.Ordinal);
        foreach (var file in files)
        {
            string code = File.ReadAllText(file).Replace("\r\n", "\n");
            string title = Regex.Match(code, @"^// Title: (.+)$", RegexOptions.Multiline).Groups[1].Value.Trim();
            string summary = Regex.Match(code, @"^// Summary: (.+)$", RegexOptions.Multiline).Groups[1].Value.Trim();
            if (title.Length == 0) throw new InvalidOperationException($"{file} needs a '// Title:' line at the top.");
            string name = Path.GetFileNameWithoutExtension(file);
            list.Add(new(Path.GetRelativePath(root, file).Replace('\\', '/'), name, title, summary, code));
        }

        // Program.cs first, then the topics in the order Program.cs runs them, then the helpers, then the four pattern pages.
        string program = File.ReadAllText(Path.Combine(examplesRoot, "Program.cs"));
        int Order(Example e) => e.Slug == "Program" ? -1 : program.IndexOf(e.Slug + ".Run", StringComparison.Ordinal) is int i and >= 0 ? i : int.MaxValue;
        return list.OrderBy(e => e.Title.StartsWith("Pattern ") ? 3 : e.Title.StartsWith("Helper:") ? 2 : 1)
            .ThenBy(e => e.Title.StartsWith("Pattern ") ? 0 : Order(e))
            .ThenBy(e => e.Title, StringComparer.Ordinal).ToList();
    }

    private static string TemplatesPage(string root)
    {
        string dir = Path.Combine(root, "examples", "Toshal.Template.Examples", "Templates");
        var sb = new StringBuilder("<h1>Template files</h1>\n<p>The realistic examples keep their templates in <code>examples/Toshal.Template.Examples/Templates</code> and compile them in as embedded resources.</p>\n");
        foreach (var file in Directory.GetFiles(dir, "*.txt", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal))
        {
            string rel = Path.GetRelativePath(root, file).Replace('\\', '/');
            sb.AppendLine($"<h2>{Enc(Path.GetRelativePath(dir, file).Replace('\\', '/'))}</h2>");
            sb.AppendLine($"<pre><code class=\"lang-text\">{Enc(File.ReadAllText(file).Replace("\r\n", "\n"))}</code></pre>");
        }
        return sb.ToString();
    }

    // ---- API pages

    private static string TypeTable(ApiModel api, bool pageIsInApi)
    {
        var sb = new StringBuilder();
        foreach (var group in api.Types.GroupBy(t => t.Type.Namespace))
        {
            sb.AppendLine($"<h2>{Enc(group.Key ?? "")}</h2>");
            sb.AppendLine("<div class=\"table-wrap\"><table><tr><th>Type</th><th>Summary</th></tr>");
            foreach (var t in group)
            {
                string href = pageIsInApi ? $"{t.FileName}.html" : $"api/{t.FileName}.html";
                string obsolete = t.Obsolete ? " <span class=\"badge\">obsolete</span>" : "";
                sb.AppendLine($"<tr><td><a href=\"{href}\"><code>{Enc(t.Name)}</code></a>{obsolete}</td><td>{Summary(api, t.Doc)}</td></tr>");
            }
            sb.AppendLine("</table></div>");
        }
        return sb.ToString();
    }

    private static string TypePage(ApiModel api, ApiModel.TypeInfoEx t)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<h1>{Enc(t.Name)}</h1>");
        sb.AppendLine($"<p class=\"muted\">{Enc(t.Kind)} in <code>{Enc(t.Type.Namespace ?? "")}</code>, assembly <code>{Enc(t.Type.Assembly.GetName().Name ?? "")}</code>{(t.Obsolete ? " <span class=\"badge\">obsolete</span>" : "")}</p>");
        sb.Append(DocBody(api, t.Doc, includeParams: false));

        foreach (var (kind, heading) in new[] { ("Constructor", "Constructors"), ("Property", "Properties"), ("Field", "Fields"), ("Method", "Methods") })
        {
            var list = t.Members.Where(m => m.Kind == kind).ToList();
            if (list.Count == 0) continue;
            sb.AppendLine($"<h2>{heading}</h2>");
            foreach (var m in list)
            {
                sb.AppendLine("<section class=\"member\">");
                sb.AppendLine($"<pre class=\"sig\"><code>{Enc(m.Signature)}</code></pre>{(m.Obsolete ? "<span class=\"badge\">obsolete</span>" : "")}");
                sb.Append(DocBody(api, m.Doc, includeParams: true));
                sb.AppendLine("</section>");
            }
        }
        return sb.ToString();
    }

    private static string Summary(ApiModel api, XElement? doc)
    {
        var s = doc?.Element("summary");
        return s == null ? "" : Inline(api, s).Trim();
    }

    private static string DocBody(ApiModel api, XElement? doc, bool includeParams)
    {
        var sb = new StringBuilder();
        if (doc == null) return "<p class=\"muted\">No documentation.</p>";
        var summary = doc.Element("summary");
        if (summary != null) sb.AppendLine("<p>" + Inline(api, summary).Trim() + "</p>");

        var ps = doc.Elements("param").ToList();
        if (includeParams && ps.Count > 0)
        {
            sb.AppendLine("<h4>Parameters</h4><dl>");
            foreach (var p in ps) sb.AppendLine($"<dt><code>{Enc((string)p.Attribute("name")!)}</code></dt><dd>{Inline(api, p)}</dd>");
            sb.AppendLine("</dl>");
        }
        var returns = doc.Element("returns");
        if (returns != null) sb.AppendLine("<h4>Returns</h4><p>" + Inline(api, returns).Trim() + "</p>");
        var ex = doc.Elements("exception").ToList();
        if (ex.Count > 0)
        {
            sb.AppendLine("<h4>Exceptions</h4><dl>");
            foreach (var e in ex)
            {
                string cref = ((string?)e.Attribute("cref") ?? "").Split(':').Last().Split('.').Last();
                sb.AppendLine($"<dt><code>{Enc(cref)}</code></dt><dd>{Inline(api, e)}</dd>");
            }
            sb.AppendLine("</dl>");
        }
        var remarks = doc.Element("remarks");
        if (remarks != null) sb.AppendLine("<h4>Remarks</h4>" + Blocks(api, remarks));
        foreach (var example in doc.Elements("example"))
        {
            sb.AppendLine("<h4>Example</h4>");
            foreach (var code in example.Elements("code"))
            {
                sb.AppendLine("<pre><code class=\"lang-csharp\">" + Enc(Dedent(code.Value)) + "</code></pre>");
            }
        }
        return sb.ToString();
    }

    /// <summary>Renders remarks. Each para becomes a paragraph; one that starts with a bold "Warning" or "Known issue" becomes a highlighted box.</summary>
    private static string Blocks(ApiModel api, XElement element)
    {
        var paras = element.Elements("para").ToList();
        if (paras.Count == 0) return "<p>" + Inline(api, element).Trim() + "</p>";
        var sb = new StringBuilder();
        foreach (var para in paras)
        {
            string bold = para.Elements("b").FirstOrDefault()?.Value ?? "";
            bool warning = bold.StartsWith("Warning", StringComparison.OrdinalIgnoreCase) || bold.StartsWith("Known issue", StringComparison.OrdinalIgnoreCase);
            sb.Append(warning ? "<p class=\"warning\">" : "<p>").Append(Inline(api, para).Trim()).AppendLine("</p>");
        }
        return sb.ToString();
    }

    private static string Dedent(string code)
    {
        var lines = code.Replace("\r\n", "\n").Trim('\n').Split('\n');
        int indent = lines.Where(l => l.Trim().Length > 0).Select(l => l.Length - l.TrimStart().Length).DefaultIfEmpty(0).Min();
        return string.Join("\n", lines.Select(l => l.Length >= indent ? l[indent..] : l.TrimStart())).TrimEnd();
    }

    /// <summary>Renders the text inside a doc element: plain text, see, paramref, b and c tags.</summary>
    private static string Inline(ApiModel api, XElement element)
    {
        var sb = new StringBuilder();
        foreach (var node in element.Nodes())
        {
            switch (node)
            {
                case XText text:
                    sb.Append(Enc(Regex.Replace(text.Value, @"\s+", " ")));
                    break;
                case XElement e when e.Name == "c":
                    sb.Append("<code>").Append(Enc(e.Value)).Append("</code>");
                    break;
                case XElement e when e.Name == "b":
                    sb.Append("<strong>").Append(Enc(e.Value)).Append("</strong>");
                    break;
                case XElement e when e.Name == "paramref":
                    sb.Append("<code>").Append(Enc((string?)e.Attribute("name") ?? "")).Append("</code>");
                    break;
                case XElement e when e.Name == "see":
                    string cref = (string?)e.Attribute("cref") ?? "";
                    string shown = cref.Split(':').Last().Split('(')[0].Split('.').Last();
                    if (cref.StartsWith("T:") && api.TryGetTypePage(cref, out var file))
                    {
                        sb.Append($"<a href=\"{linkBase}{file}.html\"><code>{Enc(shown)}</code></a>");
                    }
                    else if (cref.StartsWith("M:") || cref.StartsWith("P:") || cref.StartsWith("F:"))
                    {
                        string typeId = "T:" + string.Join(".", cref[2..].Split('(')[0].Split('.').SkipLast(1));
                        if (api.TryGetTypePage(typeId, out var f2)) sb.Append($"<a href=\"{linkBase}{f2}.html\"><code>{Enc(shown)}</code></a>");
                        else sb.Append($"<code>{Enc(shown)}</code>");
                    }
                    else
                    {
                        sb.Append($"<code>{Enc(shown)}</code>");
                    }
                    break;
                case XElement e:
                    sb.Append(Inline(api, e));
                    break;
            }
        }
        return sb.ToString();
    }

    // ---- page shell

    private static readonly (string Key, string Title, string Href)[] Nav =
    {
        ("index", "Home", "index.html"),
        ("why", "Why use it", "why.html"),
        ("getting-started", "Getting started", "getting-started.html"),
        ("syntax", "Template syntax", "syntax.html"),
        ("pattern", "Testing pattern", "pattern.html"),
        ("api", "API reference", "api/index.html"),
        ("examples", "Examples", "examples/index.html"),
        ("known-issues", "Known issues", "known-issues.html"),
        ("contributing", "Contributing", "contributing.html"),
    };

    private static string Page(string title, string root, string active, string body)
    {
        var nav = string.Concat(Nav.Select(n => $"<a href=\"{root}{n.Href}\"{(n.Key == active ? " class=\"active\" aria-current=\"page\"" : "")}>{n.Title}</a>"));
        return $@"<!doctype html>
<html lang=""en"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<title>{Enc(title)} - Toshal.Template</title>
<link rel=""stylesheet"" href=""{root}assets/site.css"">
</head>
<body>
<header><div class=""wrap""><a class=""brand"" href=""{root}index.html"">Toshal.Template</a><nav>{nav}<a href=""{RepoRoot.RepoUrl}"">GitHub</a></nav></div></header>
<main class=""wrap"">
{body}
</main>
<footer><div class=""wrap"">MIT License. Copyright (c) 2026 Toshal Infotech. This page was generated by <code>docs.cmd</code>; do not edit it by hand.</div></footer>
</body>
</html>
";
    }

    private static void Write(string docs, string relative, string html)
    {
        string path = Path.Combine(docs, relative.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, html.Replace("\r\n", "\n"), new UTF8Encoding(false));
    }

    private static string Enc(string s) => WebUtility.HtmlEncode(s);

    private const string Css = @":root{--bg:#fff;--fg:#1c2330;--muted:#5b6678;--line:#d9dee7;--accent:#1f5fbf;--code:#f3f5f9}
@media (prefers-color-scheme:dark){:root{--bg:#10141b;--fg:#e4e8ef;--muted:#9aa5b8;--line:#2a3140;--accent:#7fb0ff;--code:#1a202b}}
*{box-sizing:border-box}
body{margin:0;background:var(--bg);color:var(--fg);font:16px/1.6 system-ui,-apple-system,Segoe UI,sans-serif}
.wrap{max-width:60rem;margin:0 auto;padding:0 1rem}
header{border-bottom:1px solid var(--line)}
header .wrap{display:flex;flex-wrap:wrap;align-items:center;gap:.5rem 1.5rem;padding-top:.75rem;padding-bottom:.75rem}
.brand{font-weight:700;color:var(--fg);text-decoration:none;font-size:1.15rem}
nav{display:flex;flex-wrap:wrap;gap:.25rem 1rem}
nav a{color:var(--muted);text-decoration:none}
nav a.active,nav a:hover{color:var(--accent)}
main{padding-top:1.5rem;padding-bottom:3rem}
footer{border-top:1px solid var(--line);color:var(--muted);font-size:.9rem;padding:1rem 0}
a{color:var(--accent)}
h1{font-size:2rem;margin:.5rem 0 1rem}h2{margin-top:2rem}h4{margin:1rem 0 .25rem}
code,pre{font-family:ui-monospace,Consolas,monospace;font-size:.9em}
code{background:var(--code);padding:.05rem .3rem;border-radius:.25rem}
pre{background:var(--code);padding:.75rem 1rem;border-radius:.4rem;overflow-x:auto;line-height:1.45}
pre code{background:none;padding:0}
.table-wrap{overflow-x:auto}
table{border-collapse:collapse;width:100%}
th,td{border-bottom:1px solid var(--line);padding:.4rem .6rem;text-align:left;vertical-align:top}
.muted{color:var(--muted)}
.warning{border-left:4px solid #d98e04;background:rgba(217,142,4,.12);padding:.5rem .75rem;border-radius:.25rem}
.badge{background:var(--line);border-radius:.6rem;padding:.05rem .5rem;font-size:.8rem}
.member{border-top:1px solid var(--line);padding-top:.5rem;margin-top:1rem}
.sig{margin:.5rem 0}
dl{margin:.25rem 0}dt{font-weight:600;margin-top:.25rem}dd{margin:0 0 .25rem 1.25rem}
";
}
