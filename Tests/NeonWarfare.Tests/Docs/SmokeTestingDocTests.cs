using Microsoft.CodeAnalysis.CSharp.Syntax;
using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Docs;

/// <summary>
/// The same job Docs/TestingDocTests does for the unit tests, done for the smoke ones: the "Scenarios"
/// table of Docs/Smoke-testing.md is the only inventory of what is launched and with which flags. It
/// needs it more, not less — the smoke project is not run in CI, so a scenario that was renamed or
/// deleted can stay wrong in the document for a long time with nothing pointing it out.
///
/// The unit is a test method here, not a class: the table lists Client_StartsToMenu and its siblings,
/// which all live in one class.
/// </summary>
public class SmokeTestingDocTests
{
    private const string DocumentName = "Smoke-testing.md";

    private const string ScenariosHeading = "Scenarios";

    private const string RunningHeading = "Running";

    private static readonly string[] TestMethodAttributes = ["Fact", "Theory"];

    [Fact]
    public void SmokeTests_AreListedInTheTable()
    {
        IReadOnlySet<string> documented = DocumentedScenarios();
        FailureReport report = new($"Smoke tests missing from the '{ScenariosHeading}' table of Docs/{DocumentName}");

        foreach (string test in DeclaredSmokeTests().Order(StringComparer.Ordinal))
        {
            if (!documented.Contains(test))
            {
                report.Add($"{test} — add a row saying which processes it starts");
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void TableRows_PointToExistingSmokeTests()
    {
        IReadOnlySet<string> declared = DeclaredSmokeTests();
        FailureReport report = new($"Rows of the '{ScenariosHeading}' table that no smoke test backs");

        foreach (string documented in DocumentedScenarios().Order(StringComparer.Ordinal))
        {
            if (!declared.Contains(documented))
            {
                report.Add($"{documented} — renamed or deleted, the row is stale");
            }
        }

        report.AssertEmpty();
    }

    [Fact]
    public void TableRows_NameOneTestAndDescribeIt()
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        FailureReport report = new($"Malformed rows of the '{ScenariosHeading}' table of Docs/{DocumentName}");

        foreach (IReadOnlyList<string> row in ScenariosTable().Rows)
        {
            List<string> names = MarkdownDocument.CodeSpans(row[0]).ToList();
            if (names.Count != 1)
            {
                report.Add($"'{row[0]}' — the first cell must hold exactly one test method name in backticks");
                continue;
            }

            if (row[1].Length == 0)
            {
                report.Add($"{names[0]} — the row says nothing about which processes the scenario starts");
            }

            if (!seen.Add(names[0]))
            {
                report.Add($"{names[0]} — listed twice");
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// The command under "Running" is what a reader copies to run the suite. It names a path, and a
    /// path in a fenced block is not covered by the link checks — nothing else would notice the project
    /// being moved or renamed.
    /// </summary>
    [Fact]
    public void RunCommand_NamesTheSmokeTestProject()
    {
        string expected = RepositoryPaths.Relative(RepositoryPaths.SmokeTestProjectPath);
        Assert.True(File.Exists(RepositoryPaths.SmokeTestProjectPath), $"{expected} does not exist.");

        bool named = Document().Section(RunningHeading)
            .Any(line => line.Text.Contains(expected, StringComparison.Ordinal));

        Assert.True(named,
            $"Docs/{DocumentName}: the '{RunningHeading}' section never names {expected}. " +
            $"The command a reader copies from there must point at the project that actually exists.");
    }

    /// <summary>The test method names of the table, as they are written.</summary>
    private static IReadOnlySet<string> DocumentedScenarios() =>
        ScenariosTable().Rows
            .Select(row => MarkdownDocument.CodeSpans(row[0]).FirstOrDefault())
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Every smoke test that actually exists, by method name — a method carrying [Fact] or [Theory].
    /// Infrastructure/ holds the process launching and the output scanning and runs no test of its own.
    /// </summary>
    private static IReadOnlySet<string> DeclaredSmokeTests() =>
        RepositoryPaths.SmokeTestFiles()
            .Select(CSharpFile.Load)
            .SelectMany(file => file.Nodes<MethodDeclarationSyntax>())
            .Where(IsTestMethod)
            .Select(method => method.Identifier.ValueText)
            .ToHashSet(StringComparer.Ordinal);

    private static bool IsTestMethod(MethodDeclarationSyntax method) =>
        method.AttributeLists
            .SelectMany(list => list.Attributes)
            .Any(attribute => TestMethodAttributes.Contains(CSharpFile.AttributeName(attribute)));

    private static MarkdownTable ScenariosTable() =>
        Document().TableUnder(ScenariosHeading) ?? throw new InvalidOperationException(
            $"Docs/{DocumentName}: no table under '{ScenariosHeading}'. Either the heading was renamed, " +
            $"or the inventory of scenarios is gone.");

    private static MarkdownDocument Document() =>
        MarkdownDocument.Load(Path.Combine(RepositoryPaths.DocsDirectory, DocumentName));
}
