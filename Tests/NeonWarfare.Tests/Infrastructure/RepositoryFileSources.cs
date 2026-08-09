using Xunit;

namespace NeonWarfare.Tests.Infrastructure;

/// <summary>
/// MemberData sources for the checks that do not care what a file contains — only how its bytes are
/// written. They span the whole repository, code and documentation alike, which is what separates them
/// from <see cref="GameFileSources"/> and <see cref="MarkdownFileSources"/>.
/// </summary>
public static class RepositoryFileSources
{
    /// <summary>Every hand-written text file: sources, scenes, sidecars, locales, docs, configuration.</summary>
    public static TheoryData<string> TextFiles => RepositoryPaths.TextFiles().AsTheoryData();
}
