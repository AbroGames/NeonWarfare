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
    private const string CoverageHeading = "What is covered now";

    private static readonly string[] TestMethodAttributes = ["Fact", "Theory"];

    [Fact]
    public void TestClasses_AreListedInTheTable()
    {
        IReadOnlySet<string> documented = DocumentedTestClasses();
        FailureReport report = new($"Test classes missing from the '{CoverageHeading}' table of Docs/Testing.md");

        foreach (string testClass in DeclaredTestClasses().Order(StringComparer.Ordinal))
        {
            if (!documented.Contains(testClass))
            {
                report.Add($"{testClass} — add a row saying what it checks");
            }
        }

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
        HashSet<string> seen = new(StringComparer.Ordinal);
        FailureReport report = new($"Malformed rows of the '{CoverageHeading}' table of Docs/Testing.md");

        foreach (IReadOnlyList<string> row in table.Rows)
        {
            List<string> names = MarkdownDocument.CodeSpans(row[0]).ToList();
            if (names.Count != 1)
            {
                report.Add($"'{row[0]}' — the first cell must hold exactly one class path in backticks");
                continue;
            }

            if (row[1].Length == 0)
            {
                report.Add($"{names[0]} — the row says nothing about what the class checks");
            }

            if (!seen.Add(names[0]))
            {
                report.Add($"{names[0]} — listed twice");
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// The class paths of the table, as they are written: <c>Docs/DocsLinksTests</c> — relative to
    /// Tests/NeonWarfare.Tests/, without the extension. A row with anything else in its first cell is
    /// reported by <see cref="TableRows_NameOneClassAndDescribeIt"/>, not here.
    /// </summary>
    private static IReadOnlySet<string> DocumentedTestClasses() =>
        CoverageTable().Rows
            .Select(row => MarkdownDocument.CodeSpans(row[0]).FirstOrDefault())
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Every test class that actually exists, keyed the way the table writes it. A class counts as one
    /// when it declares a [Fact] or a [Theory]: Infrastructure/ holds helpers that run no tests, and
    /// listing them as coverage would say something untrue.
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

            foreach (ClassDeclarationSyntax declaration in file.Nodes<ClassDeclarationSyntax>())
            {
                if (!DeclaresTestMethod(declaration))
                {
                    continue;
                }

                string name = declaration.Identifier.ValueText;
                classes.Add(directory == "." ? name : $"{directory}/{name}");
            }
        }

        return classes;
    }

    private static bool DeclaresTestMethod(ClassDeclarationSyntax declaration) =>
        declaration.Members.OfType<MethodDeclarationSyntax>()
            .SelectMany(method => method.AttributeLists)
            .SelectMany(list => list.Attributes)
            .Any(attribute => TestMethodAttributes.Contains(CSharpFile.AttributeName(attribute)));

    private static MarkdownTable CoverageTable()
    {
        MarkdownDocument document =
            MarkdownDocument.Load(Path.Combine(RepositoryPaths.DocsDirectory, "Testing.md"));

        return document.TableUnder(CoverageHeading) ?? throw new InvalidOperationException(
            $"Docs/Testing.md: no table under '{CoverageHeading}'. Either the heading was renamed, or " +
            $"the inventory of test classes is gone.");
    }
}
