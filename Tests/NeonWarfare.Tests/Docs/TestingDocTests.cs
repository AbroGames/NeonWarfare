using Microsoft.CodeAnalysis.CSharp.Syntax;
using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Docs;

/// <summary>
/// The "What is covered now" table of Docs/Testing.md is the only inventory of what the suite checks —
/// the document says outright that a new test means a new row. Nothing else makes that true: a class
/// added without a row is invisible to a reader deciding what still needs covering, and a row left
/// behind after a rename claims coverage that no longer exists, which is worse than no row at all.
/// </summary>
public class TestingDocTests
{
    private const string DocumentName = "Testing.md";

    private const string CoverageHeading = "What is covered now";

    private const int ClassColumn = 0;

    private const int PurposeColumn = 1;

    [Fact]
    public void TestClasses_AreListedInTheTable()
    {
        FailureReport report = new($"Test classes missing from the '{CoverageHeading}' table of Docs/{DocumentName}");

        CrossCheck.ReportMissing(
            report,
            DeclaredTestClasses().Order(StringComparer.Ordinal),
            DocumentedTestClasses(),
            testClass => $"{testClass} — add a row saying what it checks");

        report.AssertEmpty();
    }

    [Fact]
    public void TableRows_PointToExistingTestClasses()
    {
        IReadOnlySet<string> declared = DeclaredTestClasses();
        FailureReport report = new($"Rows of the '{CoverageHeading}' table that no test class backs");

        foreach (string documented in DocumentedTestClasses().Order(StringComparer.Ordinal))
        {
            if (declared.Contains(documented))
            {
                continue;
            }

            // A moved class is the common case, and "it is over there now" is a more useful failure
            // than "it does not exist" when only the folder in the row went stale.
            string name = documented[(documented.LastIndexOf('/') + 1)..];
            string? moved = declared.FirstOrDefault(
                candidate => candidate[(candidate.LastIndexOf('/') + 1)..] == name);

            report.Add(moved is null
                ? $"{documented} — renamed or deleted, the row is stale"
                : $"{documented} — the class lives at {moved} now");
        }

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

    /// <summary>
    /// The class paths of the table, as they are written: <c>Docs/DocsLinksTests</c> — relative to
    /// Tests/NeonWarfare.Tests/, without the extension. A row with anything else in its first cell is
    /// reported by <see cref="TableRows_NameOneClassAndDescribeIt"/>, not here.
    /// </summary>
    private static IReadOnlySet<string> DocumentedTestClasses() =>
        CoverageTable().CodeSpanColumn(ClassColumn).ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Every test class that actually exists, keyed the way the table writes it: the folder it lives in
    /// under Tests/NeonWarfare.Tests/, then the class name.
    /// </summary>
    private static IReadOnlySet<string> DeclaredTestClasses()
    {
        HashSet<string> classes = new(StringComparer.Ordinal);

        foreach (string path in RepositoryPaths.TestFiles())
        {
            CSharpFile file = CSharpFile.Load(path);
            string directory = Path
                .GetRelativePath(RepositoryPaths.TestsDirectory, Path.GetDirectoryName(path)!)
                .Replace(Path.DirectorySeparatorChar, '/');

            foreach (ClassDeclarationSyntax declaration in file.Nodes<ClassDeclarationSyntax>()
                         .Where(CSharpFile.DeclaresTestMethod))
            {
                string name = declaration.Identifier.ValueText;
                classes.Add(directory == "." ? name : $"{directory}/{name}");
            }
        }

        return classes;
    }

    private static MarkdownTable CoverageTable() =>
        MarkdownDocument.LoadDoc(DocumentName)
            .Section(CoverageHeading)
            .RequireTable("the inventory of test classes is gone", "Test class", "What it checks");
}
