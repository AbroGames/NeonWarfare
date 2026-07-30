using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Docs;

/// <summary>
/// Every link written in the documentation has to resolve: the file (or directory) it points at must
/// exist, and if the link carries an anchor, the target file must actually have that heading.
/// </summary>
public class DocsLinksTests
{
    [Theory]
    [MemberData(nameof(MarkdownFileSources.DocsAndReadme), MemberType = typeof(MarkdownFileSources))]
    public void Links_PointToExistingFiles(string relativePath)
    {
        MarkdownDocument document = MarkdownDocument.Load(RepositoryPaths.Absolute(relativePath));
        FailureReport report = new($"{relativePath}: links to missing files");

        foreach (MarkdownLink link in document.Links)
        {
            if (link.IsExternal || link.IsAnchorOnly)
            {
                continue;
            }

            if (link.PathPart.Length == 0)
            {
                report.Add($"{link.Describe()}: empty link target");
                continue;
            }

            string target = ResolveTarget(document.Path, link.PathPart);

            // Directory links are legitimate — README.md points at Scenes/Game/Starters/ and .run/.
            if (!File.Exists(target) && !Directory.Exists(target))
            {
                report.Add($"{link.Describe()}: {RepositoryPaths.Relative(target)} does not exist");
                continue;
            }

            if (!IsInsideRepository(target))
            {
                report.Add($"{link.Describe()}: resolves outside the repository");
            }
        }

        report.AssertEmpty();
    }

    [Theory]
    [MemberData(nameof(MarkdownFileSources.DocsAndReadme), MemberType = typeof(MarkdownFileSources))]
    public void Links_PointToExistingAnchors(string relativePath)
    {
        MarkdownDocument document = MarkdownDocument.Load(RepositoryPaths.Absolute(relativePath));
        FailureReport report = new($"{relativePath}: links to missing sections");

        foreach (MarkdownLink link in document.Links)
        {
            if (link.IsExternal || link.Anchor.Length == 0)
            {
                continue;
            }

            string target = link.IsAnchorOnly
                ? document.Path
                : ResolveTarget(document.Path, link.PathPart);

            if (!File.Exists(target))
            {
                // Missing files are reported by Links_PointToExistingFiles; nothing to check here.
                continue;
            }

            if (!target.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                report.Add($"{link.Describe()}: anchors are only meaningful for .md files");
                continue;
            }

            MarkdownDocument targetDocument = MarkdownDocument.Load(target);
            if (!targetDocument.HeadingSlugs.Contains(link.Anchor))
            {
                string known = targetDocument.HeadingSlugs.Count == 0
                    ? "<no headings>"
                    : string.Join(", ", targetDocument.HeadingSlugs);
                report.Add($"{link.Describe()}: no heading with anchor '{link.Anchor}'. Known: {known}");
            }
        }

        report.AssertEmpty();
    }

    private static string ResolveTarget(string sourceFile, string pathPart)
    {
        string sourceDirectory = Path.GetDirectoryName(sourceFile)!;
        return Path.GetFullPath(Path.Combine(sourceDirectory, pathPart));
    }

    private static bool IsInsideRepository(string absolutePath) =>
        absolutePath.StartsWith(RepositoryPaths.Root, StringComparison.Ordinal);
}
