using Microsoft.CodeAnalysis.CSharp.Syntax;
using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Docs;

/// <summary>
/// The same job Docs/TestingDocTests does for the unit tests, done for the game ones: the "What is covered
/// now" table of Docs/Game-testing.md is the only inventory of what runs inside the engine. A test class
/// is found by its <c>[TestSuite]</c> rather than by its methods: gdUnit4 marks them <c>[TestCase]</c>, and
/// Fixtures/ holds node classes that run nothing.
/// </summary>
public class GameTestingDocTests
{
    private const string DocumentName = "Game-testing.md";

    private const string CoverageHeading = "What is covered now";

    private const string SuiteAttribute = "TestSuite";

    private const int ClassColumn = 0;

    private const int PurposeColumn = 1;

    [Fact]
    public void TestSuites_AreListedInTheTable()
    {
        FailureReport report = new(
            $"Game test classes missing from the '{CoverageHeading}' table of Docs/{DocumentName}");

        CrossCheck.ReportMissing(
            report,
            DeclaredTestSuites().Order(StringComparer.Ordinal),
            DocumentedTestSuites(),
            testClass => $"{testClass} — add a row saying what it checks");

        report.AssertEmpty();
    }

    [Fact]
    public void TableRows_PointToExistingTestSuites()
    {
        FailureReport report = new($"Rows of the '{CoverageHeading}' table that no game test class backs");

        CrossCheck.ReportMissing(
            report,
            DocumentedTestSuites().Order(StringComparer.Ordinal),
            DeclaredTestSuites(),
            testClass => $"{testClass} — renamed, moved or deleted, the row is stale");

        report.AssertEmpty();
    }

    [Fact]
    public void TableRows_NameOneClassAndDescribeIt()
    {
        MarkdownTable table = CoverageTable();
        FailureReport report = new($"Malformed rows of the '{CoverageHeading}' table of Docs/{DocumentName}");

        DocTableChecks.SingleCodeSpanPerRow(table, report, ClassColumn, "class path");
        DocTableChecks.CellIsNotEmpty(table, report, PurposeColumn, ClassColumn, "what the class checks");

        report.AssertEmpty();
    }

    private static IReadOnlySet<string> DocumentedTestSuites() =>
        CoverageTable().CodeSpanColumn(ClassColumn).ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Every <c>[TestSuite]</c> class, keyed the way the table writes it: the folder it lives in under
    /// Tests/NeonWarfare.GameTests/, then the class name.
    /// </summary>
    private static IReadOnlySet<string> DeclaredTestSuites()
    {
        HashSet<string> classes = new(StringComparer.Ordinal);

        foreach (string path in RepositoryPaths.GameTestFiles())
        {
            CSharpFile file = CSharpFile.Load(path);
            string directory = Path
                .GetRelativePath(RepositoryPaths.GameTestsDirectory, Path.GetDirectoryName(path)!)
                .Replace(Path.DirectorySeparatorChar, '/');

            foreach (ClassDeclarationSyntax declaration in file.Nodes<ClassDeclarationSyntax>()
                         .Where(IsTestSuite))
            {
                string name = declaration.Identifier.ValueText;
                classes.Add(directory == "." ? name : $"{directory}/{name}");
            }
        }

        return classes;
    }

    private static bool IsTestSuite(ClassDeclarationSyntax declaration) =>
        declaration.AttributeLists
            .SelectMany(list => list.Attributes)
            .Any(attribute => CSharpFile.AttributeName(attribute) == SuiteAttribute);

    private static MarkdownTable CoverageTable() =>
        MarkdownDocument.LoadDoc(DocumentName)
            .Section(CoverageHeading)
            .RequireTable("the inventory of game test classes is gone", "Test class", "What it checks");
}
