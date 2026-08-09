using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Docs;

/// <summary>
/// README.md is the single entry point into the documentation, and Docs/Repository-structure.md
/// states it outright: "на все файлы есть ссылки из README.md". A new file under Docs/ that nobody
/// links to is invisible, so this test refuses to let one appear.
/// </summary>
public class DocsReadmeIndexTests
{
    [Fact]
    public void EveryDocFile_IsLinkedFromReadme()
    {
        MarkdownDocument readme = MarkdownDocument.Load(RepositoryPaths.ReadmePath);
        string readmeDirectory = Path.GetDirectoryName(readme.Path)!;

        HashSet<string> linked = readme.Links
            .Where(link => !link.IsExternal && !link.IsAnchorOnly && link.PathPart.Length > 0)
            .Select(link => Path.GetFullPath(Path.Combine(readmeDirectory, link.PathPart)))
            .ToHashSet(StringComparer.Ordinal);

        FailureReport report = new("Docs files not linked from README.md");
        foreach (string docFile in RepositoryPaths.DocFiles())
        {
            if (!linked.Contains(docFile))
            {
                report.Add($"{RepositoryPaths.Relative(docFile)}: add a row linking to it in README.md");
            }
        }

        report.AssertEmpty();
    }
}
