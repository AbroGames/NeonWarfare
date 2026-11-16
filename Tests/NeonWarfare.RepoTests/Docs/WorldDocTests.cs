using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Docs;

/// <summary>
/// The folder table of Docs/World.md is the map of the World machinery: the services of <c>World/Infra/</c> are found
/// by their attributes and never named together anywhere in the code, so a new folder, or a renamed one, leaves the
/// document behind silently. The inside of <c>Features/</c> is the table of Docs/World-features.md.
/// </summary>
public class WorldDocTests
{
    private const string DocumentName = "World.md";

    private const string FoldersHeading = "Folders";

    private const string FeaturesFolder = "Features";

    private const int FolderColumn = 0;

    private const int PurposeColumn = 1;

    [Fact]
    public void WorldFolders_AreListedInTheTable()
    {
        FailureReport report = new($"Folders of World missing from the table of Docs/{DocumentName}");

        CrossCheck.ReportMissing(
            report,
            DeclaredFolders().Order(StringComparer.Ordinal),
            DocumentedFolders().ToHashSet(StringComparer.Ordinal),
            folder => $"{folder} — add a row saying what it holds");

        report.AssertEmpty();
    }

    [Fact]
    public void TableRows_PointToExistingFolders()
    {
        FailureReport report = new($"Rows of Docs/{DocumentName} that no folder of World backs");

        CrossCheck.ReportMissing(
            report,
            DocumentedFolders(),
            DeclaredFolders(),
            folder => $"{folder} — renamed, moved or deleted, the row is stale");

        report.AssertEmpty();
    }

    [Fact]
    public void TableRows_NameOneFolderAndDescribeIt()
    {
        MarkdownTable table = Table();
        FailureReport report = new($"Malformed rows of the folder table of Docs/{DocumentName}");

        DocTableChecks.SingleCodeSpanPerRow(table, report, FolderColumn, "folder path");
        DocTableChecks.CellIsNotEmpty(table, report, PurposeColumn, FolderColumn, "what the folder holds");

        report.AssertEmpty();
    }

    /// <summary>Every folder of World relative to it, <c>Features</c> itself included but nothing inside it.</summary>
    private static IReadOnlySet<string> DeclaredFolders() =>
        RepositoryPaths.FoldersUnder(RepositoryPaths.WorldDirectory)
            .Where(folder => !folder.StartsWith(FeaturesFolder + "/", StringComparison.Ordinal))
            .ToHashSet(StringComparer.Ordinal);

    private static IReadOnlyList<string> DocumentedFolders() => Table().CodeSpanColumn(FolderColumn).ToList();

    private static MarkdownTable Table() =>
        MarkdownDocument.LoadDoc(DocumentName)
            .Section(FoldersHeading)
            .RequireTable("the map of the World machinery is gone", "Folder", "What it holds");
}
