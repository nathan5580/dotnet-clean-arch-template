namespace Architecture.Tests;

/// <summary>
/// Locates the repository root (directory containing the .slnx) and enumerates
/// first-party C# source files, excluding generated output.
/// </summary>
public static class SourceFiles
{
    private static readonly string[] ExcludedSegments =
    [
        $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
        $"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}",
    ];

    public static string RepoRoot { get; } = FindRepoRoot();

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            if (dir.GetFiles("*.slnx").Length > 0)
                return dir.FullName;

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repo root (no .slnx found walking up from test bin).");
    }

    public static IReadOnlyList<string> All() =>
        Directory.EnumerateFiles(RepoRoot, "*.cs", SearchOption.AllDirectories)
            .Where(p => !ExcludedSegments.Any(p.Contains))
            .ToList();

    public static IReadOnlyList<string> InProject(string relativeFolder)
    {
        var needle = $"{Path.DirectorySeparatorChar}{relativeFolder}{Path.DirectorySeparatorChar}";
        return All().Where(p => p.Contains(needle)).ToList();
    }

    public static IReadOnlyList<string> EndingWith(string suffix) =>
        All().Where(p => p.EndsWith(suffix, StringComparison.Ordinal)).ToList();

    public static CompilationUnitSyntax Parse(string path) =>
        CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetCompilationUnitRoot();
}
