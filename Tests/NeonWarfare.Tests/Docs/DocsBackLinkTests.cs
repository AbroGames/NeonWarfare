using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Docs;

/// <summary>
/// Every documentation file opens the same way: an H1, a blank line, a back-link to README.md, a
/// blank line. The back-link is identical everywhere except for the number of "../" segments, so it
/// is checked by exact match.
/// </summary>
public class DocsBackLinkTests
{
    private const string BackLinkTemplate = "[← README проекта]({Path})";

    [Theory]
    [MemberData(nameof(MarkdownFileSources.Docs), MemberType = typeof(MarkdownFileSources))]
    public void DocFile_StartsWithHeadingAndBackLink(string relativePath)
    {
        string absolutePath = RepositoryPaths.Absolute(relativePath);
        MarkdownDocument document = MarkdownDocument.Load(absolutePath);
        FailureReport report = new($"{relativePath}: broken documentation header");

        if (document.Lines.Length < 4)
        {
            report.Add("file is shorter than the mandatory 4-line header");
            report.AssertEmpty();
            return;
        }

        if (!document.Lines[0].StartsWith("# ", StringComparison.Ordinal))
        {
            report.Add($"line 1 must be an H1 heading, found: '{document.Lines[0]}'");
        }

        if (document.Lines[1].Length != 0)
        {
            report.Add($"line 2 must be empty, found: '{document.Lines[1]}'");
        }

        string expectedBackLink = ExpectedBackLink(absolutePath);
        if (!string.Equals(document.Lines[2], expectedBackLink, StringComparison.Ordinal))
        {
            report.Add($"line 3 must be exactly '{expectedBackLink}', found: '{document.Lines[2]}'");
        }

        if (document.Lines[3].Length != 0)
        {
            report.Add($"line 4 must be empty, found: '{document.Lines[3]}'");
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// The number of "../" segments is derived from the file's depth rather than hardcoded, so the
    /// rule survives a new subdirectory appearing under Docs/.
    /// </summary>
    private static string ExpectedBackLink(string absolutePath)
    {
        string directory = Path.GetDirectoryName(absolutePath)!;
        string toReadme = Path.GetRelativePath(directory, RepositoryPaths.ReadmePath)
            .Replace(Path.DirectorySeparatorChar, '/');
        return BackLinkTemplate.Replace("{Path}", toReadme);
    }
}
