namespace Toshal.Template.Tools;

internal static class RepoRoot
{
    public const string RepoUrl = "https://github.com/AgrawalAshishS/Toshal.Template";
    public const string Branch = "main";

    /// <summary>Walks up from the current folder until it finds Toshal.Template.sln.</summary>
    public static string Find()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Toshal.Template.sln")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("Run this tool from inside the repository (Toshal.Template.sln was not found).");
    }
}
