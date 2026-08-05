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

    private const int TestColumn = 0;

    private const int ProcessesColumn = 1;

    [Fact]
    public void SmokeTests_AreListedInTheTable()
    {
        FailureReport report = new($"Smoke tests missing from the '{ScenariosHeading}' table of Docs/{DocumentName}");

        CrossCheck.ReportMissing(
            report,
            DeclaredSmokeTests().Order(StringComparer.Ordinal),
            DocumentedScenarios(),
            test => $"{test} — add a row saying which processes it starts");

        report.AssertEmpty();
    }

    [Fact]
    public void TableRows_PointToExistingSmokeTests()
    {
        FailureReport report = new($"Rows of the '{ScenariosHeading}' table that no smoke test backs");

        CrossCheck.ReportMissing(
            report,
            DocumentedScenarios().Order(StringComparer.Ordinal),
            DeclaredSmokeTests(),
            test => $"{test} — renamed or deleted, the row is stale");

        report.AssertEmpty();
    }

    [Fact]
    public void TableRows_NameOneTestAndDescribeIt()
    {
        MarkdownTable table = ScenariosTable();
        FailureReport report = new($"Malformed rows of the '{ScenariosHeading}' table of Docs/{DocumentName}");

        DocTableChecks.SingleCodeSpanPerRow(table, report, TestColumn, "test method name");
        DocTableChecks.CellIsNotEmpty(
            table, report, ProcessesColumn, TestColumn, "which processes the scenario starts");

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

        bool named = MarkdownDocument.LoadDoc(DocumentName).Section(RunningHeading).Lines
            .Any(line => line.Text.Contains(expected, StringComparison.Ordinal));

        Assert.True(named,
            $"Docs/{DocumentName}: the '{RunningHeading}' section never names {expected}. " +
            $"The command a reader copies from there must point at the project that actually exists.");
    }

    /// <summary>The test method names of the table, as they are written.</summary>
    private static IReadOnlySet<string> DocumentedScenarios() =>
        ScenariosTable().CodeSpanColumn(TestColumn).ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Every smoke test that actually exists, by method name. Infrastructure/ holds the process
    /// launching and the output scanning and runs no test of its own.
    /// </summary>
    private static IReadOnlySet<string> DeclaredSmokeTests() =>
        RepositoryPaths.SmokeTestFiles()
            .Select(CSharpFile.Load)
            .SelectMany(file => file.Nodes<MethodDeclarationSyntax>())
            .Where(CSharpFile.IsTestMethod)
            .Select(method => method.Identifier.ValueText)
            .ToHashSet(StringComparer.Ordinal);

    private static MarkdownTable ScenariosTable() =>
        MarkdownDocument.LoadDoc(DocumentName)
            .Section(ScenariosHeading)
            .RequireTable("the inventory of scenarios is gone", "Test", "Processes");
}
