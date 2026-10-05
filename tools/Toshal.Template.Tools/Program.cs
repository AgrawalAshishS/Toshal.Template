// Small helper tool for this repo.
//   coverage    reads the coverlet result, prints a per-class table and writes coverage/index.html
using Toshal.Template.Tools;

string command = args.Length > 0 ? args[0] : "";
string root = RepoRoot.Find();

switch (command)
{
    case "coverage":
        return CoverageReport.Run(root, args.Length > 1 ? double.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture) : 90);
    default:
        Console.Error.WriteLine("Usage: Toshal.Template.Tools coverage [minimumPercent]");
        return 2;
}
