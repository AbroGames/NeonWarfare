using NeonWarfare.RepoTests.Infrastructure;
using Xunit;

namespace NeonWarfare.RepoTests.Docs;

/// <summary>
/// The folder table of Docs/World-features.md is the only list of the features: the composition root finds their
/// classes by attribute, so no code names a feature, and a new folder or a renamed one leaves the table behind.
/// </summary>
public class WorldFeaturesDocTests
{
    private const string DocumentName = "World-features.md";

    private const string FoldersHeading = "Folders";

    private const int FolderColumn = 0;

    private const int PurposeColumn = 1;

    [Fact]
    public void FeatureFolders_AreListedInTheTable()
    {
        FailureReport report = new($"Folders of World/Features missing from the table of Docs/{DocumentName}");

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
        FailureReport report = new($"Rows of Docs/{DocumentName} that no folder of World/Features backs");

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

    private static IReadOnlySet<string> DeclaredFolders() =>
        RepositoryPaths.FoldersUnder(RepositoryPaths.WorldFeaturesDirectory);

    private static IReadOnlyList<string> DocumentedFolders() => Table().CodeSpanColumn(FolderColumn).ToList();

    private static MarkdownTable Table() =>
        MarkdownDocument.LoadDoc(DocumentName)
            .Section(FoldersHeading)
            .RequireTable("the list of features is gone", "Folder", "What it holds");
}
