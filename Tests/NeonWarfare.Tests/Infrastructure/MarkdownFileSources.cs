using Xunit;

namespace NeonWarfare.Tests.Infrastructure;

/// <summary>
/// MemberData sources for the doc tests. Repository-relative paths are used so that a failing test
/// is named after the file it failed on.
/// </summary>
public static class MarkdownFileSources
{
    /// <summary>Every file under Docs/.</summary>
    public static TheoryData<string> Docs => RepositoryPaths.DocFiles().AsTheoryData();

    /// <summary>Every file under Docs/ plus README.md — the full scope of the doc checks.</summary>
    public static TheoryData<string> DocsAndReadme => RepositoryPaths.DocFilesAndReadme().AsTheoryData();
}
