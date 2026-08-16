using System.Text.RegularExpressions;

namespace Architecture.Tests;

public sealed class SourceRulesTests
{
    [Fact]
    public void Controllers_Contain_NoTryCatch()
    {
        foreach (var path in SourceFiles.EndingWith("Controller.cs"))
        {
            var root = SourceFiles.Parse(path);
            var tryCount = root.DescendantNodes().OfType<TryStatementSyntax>().Count();
            Assert.True(tryCount == 0, $"{path} contains {tryCount} try block(s). Controllers must not use try/catch.");
        }
    }

    [Fact]
    public void Solution_Contains_NoAsyncVoid()
    {
        foreach (var path in SourceFiles.All())
        {
            var root = SourceFiles.Parse(path);

            var asyncVoid = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
                .Where(m => m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.AsyncKeyword)))
                .Where(m => m.ReturnType is PredefinedTypeSyntax p && p.Keyword.IsKind(SyntaxKind.VoidKeyword));

            foreach (var method in asyncVoid)
                Assert.Fail($"{path}: '{method.Identifier.Text}' is 'async void'. Use 'async Task'.");
        }
    }

    [Fact]
    public void AllFiles_Use_FileScopedNamespaces()
    {
        foreach (var path in SourceFiles.All())
        {
            var root = SourceFiles.Parse(path);
            var blockScoped = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().ToList();
            Assert.True(blockScoped.Count == 0, $"{path} uses a block-scoped namespace. Use file-scoped (namespace X;).");
        }
    }

    [Fact]
    public void LibraryAwaits_Use_ConfigureAwaitFalse()
    {
        var libraryFiles = SourceFiles.InProject("Shared").Concat(SourceFiles.InProject("Databases"));

        foreach (var path in libraryFiles)
        {
            var root = SourceFiles.Parse(path);

            foreach (var awaitExpr in root.DescendantNodes().OfType<AwaitExpressionSyntax>())
            {
                var ok = awaitExpr.Expression is InvocationExpressionSyntax inv
                    && inv.Expression is MemberAccessExpressionSyntax member
                    && member.Name.Identifier.Text == "ConfigureAwait";

                var line = awaitExpr.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                Assert.True(ok, $"{path}: await at line {line} must use .ConfigureAwait(false).");
            }
        }
    }

    [Fact]
    public void MethodBodies_Separate_LogicalStages()
    {
        var offenders = new List<string>();

        foreach (var path in SourceFiles.All())
        {
            var root = SourceFiles.Parse(path);

            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                if (method.Body is not { } body)
                    continue;

                var betweenBraces = body.Statements.Count > 0
                    ? body.Statements[0].GetLeadingTrivia().ToFullString()
                    : body.CloseBraceToken.LeadingTrivia.ToFullString();
                var hasBlankAfterOpen = HasEmptyLine(
                    body.OpenBraceToken.TrailingTrivia.ToFullString() + betweenBraces);

                var beforeClose = body.Statements.Count > 0
                    ? body.Statements[^1].GetTrailingTrivia().ToFullString()
                    : body.OpenBraceToken.TrailingTrivia.ToFullString();
                var hasBlankBeforeClose = HasEmptyLine(beforeClose + body.CloseBraceToken.LeadingTrivia.ToFullString());

                var hasStageBreak = false;
                for (var index = 0; index < body.Statements.Count - 1; index++)
                {
                    var between = body.Statements[index].GetTrailingTrivia().ToFullString()
                        + body.Statements[index + 1].GetLeadingTrivia().ToFullString();
                    if (HasEmptyLine(between))
                    {
                        hasStageBreak = true;
                        break;
                    }
                }

                var mustSeparateStages = body.Statements.Count > 1 && !hasStageBreak;
                if (!hasBlankAfterOpen && !hasBlankBeforeClose && !mustSeparateStages)
                    continue;

                var reasons = new List<string>();
                if (hasBlankAfterOpen)
                    reasons.Add("blank after open brace");
                if (hasBlankBeforeClose)
                    reasons.Add("blank before close brace");
                if (mustSeparateStages)
                    reasons.Add("no blank line between stages");

                var line = method.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
                offenders.Add($"{path}:{line} {method.Identifier.Text} ({string.Join(", ", reasons)})");
            }
        }

        Assert.True(offenders.Count == 0,
            $"These method bodies do not follow stage aeration (blank line between logical stages, none at the braces):\n{string.Join("\n", offenders)}");
    }

    private static bool HasEmptyLine(string text) =>
        EmptyLine.IsMatch(text);

    private static readonly Regex EmptyLine = new(@"\n[ \t]*\n", RegexOptions.Compiled);
}
