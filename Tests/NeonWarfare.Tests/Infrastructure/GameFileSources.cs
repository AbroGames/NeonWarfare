using Xunit;

namespace NeonWarfare.Tests.Infrastructure;

/// <summary>
/// MemberData sources for the checks that go over the game itself — its sources, scenes and locales.
/// The counterpart of <see cref="MarkdownFileSources"/> for the documentation.
/// </summary>
public static class GameFileSources
{
    /// <summary>Every .cs file under Scenes/ and Scripts/.</summary>
    public static TheoryData<string> Sources => RepositoryPaths.SourceFiles().AsTheoryData();

    /// <summary>Every .tscn plus the standalone .tres resources.</summary>
    public static TheoryData<string> Resources => RepositoryPaths.ResourceFiles().AsTheoryData();

    /// <summary>Every .tscn — the files that have nodes, and therefore a root node.</summary>
    public static TheoryData<string> Scenes => RepositoryPaths.SceneFiles().AsTheoryData();

    /// <summary>Every .uid and .import — the files Godot keeps next to another file and for it.</summary>
    public static TheoryData<string> Sidecars =>
        RepositoryPaths.UidFiles().Concat(RepositoryPaths.ImportFiles()).AsTheoryData();

    /// <summary>en.po, ru.po and messages.pot.</summary>
    public static TheoryData<string> Locales => RepositoryPaths.LocaleFilesAndTemplate().AsTheoryData();

    /// <summary>en.po and ru.po — the files that must actually be translated.</summary>
    public static TheoryData<string> Translations => RepositoryPaths.LocaleFiles().AsTheoryData();
}
