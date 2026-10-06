// Small helper tool for this repo.
//   docs        builds the website in docs/ from the XML comments, the example files and the Markdown files in content/
//   docs check  builds the website in a temp folder and fails when docs/ differs (the docs are out of date)
//   coverage    reads the coverlet result, prints a per-class table and writes coverage/index.html
using Toshal.Template.Tools;

string command = args.Length > 0 ? args[0] : "";
string root = RepoRoot.Find();

switch (command)
{
    case "docs":
        return args.Length > 1 && args[1] == "check" ? SiteBuilder.Check(root) : SiteBuilder.Run(root, Path.Combine(root, "docs"));
    case "coverage":
        return CoverageReport.Run(root, args.Length > 1 ? double.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture) : 90);
    default:
        Console.Error.WriteLine("Usage: Toshal.Template.Tools docs [check] | coverage [minimumPercent]");
        return 2;
}
