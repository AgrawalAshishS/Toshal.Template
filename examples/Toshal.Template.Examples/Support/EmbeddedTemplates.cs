// Title: Helper: templates as embedded files
// Summary: Keep each template in its own .txt file, compile the files into the assembly as embedded resources, and parse each one once. The csproj line is <EmbeddedResource Include="Templates\**\*.txt" />.

using System.Collections.Concurrent;
using System.Reflection;
using Toshal.Template.Tokens;

namespace Toshal.Template.Examples.Support;

public static class EmbeddedTemplates
{
    private static readonly Assembly Assembly = typeof(EmbeddedTemplates).Assembly;

    // Parsed tokens do not change while they are processed, so one parse per template is enough.
    private static readonly ConcurrentDictionary<string, List<IToken>> Cache = new();

    /// <summary>The text of Templates/{path}. Use / between folders, for example "Project/Readme.txt".</summary>
    public static string Text(string path)
    {
        // The resource name is the default namespace plus the folder path with dots: Toshal.Template.Examples.Templates.Project.Readme.txt
        string name = "Toshal.Template.Examples.Templates." + path.Replace('/', '.');
        using var stream = Assembly.GetManifestResourceStream(name) ?? throw new FileNotFoundException("Embedded template not found: " + name);
        using var reader = new StreamReader(stream);

        // Git may check the file out with CRLF or LF line breaks. Use LF so the output is the same on every machine.
        return reader.ReadToEnd().Replace("\r\n", "\n");
    }

    /// <summary>The parsed tokens of Templates/{path}, parsed on the first call only. A new Parser per call keeps it safe for threads.</summary>
    public static List<IToken> Tokens(string path) => Cache.GetOrAdd(path, p => new Parser().Parse(Text(p)));

    /// <summary>The names of all embedded templates under a folder, for example "Project/".</summary>
    public static IEnumerable<string> List(string folder)
    {
        string prefix = "Toshal.Template.Examples.Templates." + folder.Replace('/', '.');
        return Assembly.GetManifestResourceNames().Where(n => n.StartsWith(prefix)).Select(n => n[prefix.Length..]).Order();
    }
}
