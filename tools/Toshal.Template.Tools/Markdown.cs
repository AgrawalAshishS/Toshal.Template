using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Toshal.Template.Tools;

/// <summary>A very small Markdown to HTML converter. It supports what the pages of this site use.</summary>
internal static class Markdown
{
    /// <summary>
    /// Converts Markdown to HTML. A line like <c>{{include:examples/File.cs}}</c> is replaced by the file as a code block.
    /// Relative links are turned into links to the repository on GitHub.
    /// </summary>
    public static string ToHtml(string markdown, string repoRoot)
    {
        var lines = markdown.Replace("\r\n", "\n").Split('\n');
        var html = new StringBuilder();
        var para = new List<string>();
        int i = 0;

        void FlushPara()
        {
            if (para.Count > 0)
            {
                html.AppendLine("<p>" + Inline(string.Join(" ", para)) + "</p>");
                para.Clear();
            }
        }

        while (i < lines.Length)
        {
            string line = lines[i];
            string trimmed = line.Trim();

            // A one line HTML comment (the markers of generated blocks) is not shown.
            if (trimmed.StartsWith("<!--") && trimmed.EndsWith("-->"))
            {
                FlushPara();
                i++;
                continue;
            }

            var include = Regex.Match(trimmed, @"^\{\{include:(.+)\}\}$");
            if (include.Success)
            {
                FlushPara();
                string code = File.ReadAllText(Path.Combine(repoRoot, include.Groups[1].Value.Trim()));
                html.AppendLine("<pre><code class=\"lang-csharp\">" + WebUtility.HtmlEncode(code.TrimEnd()) + "</code></pre>");
                i++;
                continue;
            }

            if (trimmed.StartsWith("```"))
            {
                FlushPara();
                var code = new List<string>();
                i++;
                while (i < lines.Length && !lines[i].TrimStart().StartsWith("```")) code.Add(lines[i++]);
                i++;
                string lang = trimmed.Length > 3 ? trimmed[3..].Trim() : "text";
                html.AppendLine($"<pre><code class=\"lang-{WebUtility.HtmlEncode(lang)}\">" + WebUtility.HtmlEncode(string.Join("\n", code)) + "</code></pre>");
                continue;
            }

            var heading = Regex.Match(trimmed, @"^(#{1,4})\s+(.*)$");
            if (heading.Success)
            {
                FlushPara();
                int level = heading.Groups[1].Length;
                html.AppendLine($"<h{level}>{Inline(heading.Groups[2].Value)}</h{level}>");
                i++;
                continue;
            }

            if (trimmed.StartsWith("|"))
            {
                FlushPara();
                var rows = new List<string>();
                while (i < lines.Length && lines[i].Trim().StartsWith("|")) rows.Add(lines[i++].Trim());
                html.AppendLine("<div class=\"table-wrap\"><table>");
                for (int r = 0; r < rows.Count; r++)
                {
                    var cells = SplitRow(rows[r]);
                    if (r == 1 && cells.All(c => Regex.IsMatch(c, @"^:?-{2,}:?$"))) continue;
                    string tag = r == 0 ? "th" : "td";
                    html.AppendLine("<tr>" + string.Concat(cells.Select(c => $"<{tag}>{Inline(c)}</{tag}>")) + "</tr>");
                }
                html.AppendLine("</table></div>");
                continue;
            }

            if (Regex.IsMatch(trimmed, @"^([-*]|\d+\.)\s+"))
            {
                FlushPara();
                bool ordered = char.IsDigit(trimmed[0]);
                html.AppendLine(ordered ? "<ol>" : "<ul>");
                while (i < lines.Length && Regex.IsMatch(lines[i].Trim(), @"^([-*]|\d+\.)\s+"))
                {
                    string item = Regex.Replace(lines[i].Trim(), @"^([-*]|\d+\.)\s+", "");
                    i++;
                    while (i < lines.Length && lines[i].StartsWith("  ") && lines[i].Trim().Length > 0 && !Regex.IsMatch(lines[i].Trim(), @"^([-*]|\d+\.)\s+"))
                    {
                        item += " " + lines[i].Trim();
                        i++;
                    }
                    html.AppendLine("<li>" + Inline(item) + "</li>");
                }
                html.AppendLine(ordered ? "</ol>" : "</ul>");
                continue;
            }

            if (trimmed.Length == 0)
            {
                FlushPara();
                i++;
                continue;
            }

            para.Add(trimmed);
            i++;
        }

        FlushPara();
        return html.ToString();
    }

    private static List<string> SplitRow(string row)
    {
        row = row.Trim().Trim('|');
        var cells = new List<string>();
        var cur = new StringBuilder();
        bool code = false;
        foreach (char ch in row)
        {
            if (ch == '`') code = !code;
            if (ch == '|' && !code)
            {
                cells.Add(cur.ToString().Trim());
                cur.Clear();
            }
            else
            {
                cur.Append(ch);
            }
        }
        cells.Add(cur.ToString().Trim());
        return cells;
    }

    private static string Inline(string text)
    {
        // Pull out code spans first so their content is not touched.
        var codes = new List<string>();
        text = Regex.Replace(text, "`([^`]+)`", m =>
        {
            codes.Add("<code>" + WebUtility.HtmlEncode(m.Groups[1].Value) + "</code>");
            return "\u0001" + (codes.Count - 1) + "\u0002";
        });
        text = WebUtility.HtmlEncode(text);
        text = Regex.Replace(text, @"\*\*([^*]+)\*\*", "<strong>$1</strong>");
        text = Regex.Replace(text, @"!\[([^\]]*)\]\(([^)]+)\)", m => $"<img src=\"{m.Groups[2].Value}\" alt=\"{m.Groups[1].Value}\" loading=\"lazy\">");
        text = Regex.Replace(text, @"\[([^\]]+)\]\(([^)]+)\)", m => $"<a href=\"{FixLink(WebUtility.HtmlDecode(m.Groups[2].Value))}\">{m.Groups[1].Value}</a>");
        text = Regex.Replace(text, "\u0001(\\d+)\u0002", m => codes[int.Parse(m.Groups[1].Value)]);
        return text;
    }

    private static string FixLink(string url)
    {
        if (Regex.IsMatch(url, @"^[\w/.-]+\.html(#.*)?$") && !url.StartsWith("docs/")) return WebUtility.HtmlEncode(url);
        if (url.StartsWith("http://") || url.StartsWith("https://") || url.StartsWith("#") || url.StartsWith("mailto:")) return WebUtility.HtmlEncode(url);
        return WebUtility.HtmlEncode($"{RepoRoot.RepoUrl}/blob/{RepoRoot.Branch}/" + url.TrimStart('.', '/'));
    }
}
