namespace Architecture.Tests;

public sealed class StructuralRulesTests
{
    [Fact]
    public void RepoRoot_Resolves_ToSlnxDirectory()
    {
        Assert.True(Directory.GetFiles(SourceFiles.RepoRoot, "*.slnx").Length > 0);
    }
}
