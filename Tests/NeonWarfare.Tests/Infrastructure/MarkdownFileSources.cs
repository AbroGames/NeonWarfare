using Xunit;

namespace NeonWarfare.Tests.Infrastructure;

/// <summary>
/// MemberData sources for the doc tests. Repository-relative paths are used so that a failing test
/// is named after the file it failed on.
/// </summary>
public static class MarkdownFileSources
{
    /// <summary>Every file under Docs/.</summary>
    public static TheoryData<string> Docs => Build(RepositoryPaths.DocFiles());

    /// <summary>Every file under Docs/ plus README.md — the full scope of the doc checks.</summary>
    public static TheoryData<string> DocsAndReadme => Build(RepositoryPaths.DocFilesAndReadme());

    private static TheoryData<string> Build(IEnumerable<string> absolutePaths)
    {
        TheoryData<string> data = [];
        foreach (string path in absolutePaths)
        {
            data.Add(RepositoryPaths.Relative(path));
        }

        return data;
    }
}
