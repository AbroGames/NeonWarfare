using Xunit;

namespace NeonWarfare.Tests.Infrastructure;

/// <summary>
/// The <c>[MemberData]</c> sources of every per-file check. A check that goes over the repository is a
/// <c>[Theory]</c> with one case per file, and the case is named by the repository-relative path — so a
/// failure points at the file without opening the message.
/// </summary>
public static class FileSources
{
    /// <summary>Every file under Docs/.</summary>
    public static TheoryData<string> Docs => RepositoryPaths.DocFiles().AsTheoryData();

    /// <summary>Every file under Docs/ plus README.md — the full scope of the doc checks.</summary>
    public static TheoryData<string> DocsAndReadme => RepositoryPaths.DocFilesAndReadme().AsTheoryData();

    /// <summary>Every .cs file under Scenes/ and Scripts/.</summary>
    public static TheoryData<string> Sources => RepositoryPaths.SourceFiles().AsTheoryData();

    /// <summary>Every hand-written .cs: the game, the tests and the smoke tests.</summary>
    public static TheoryData<string> CSharpFiles => RepositoryPaths.CSharpFiles().AsTheoryData();

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

    /// <summary>Every Multi-Launch configuration in .run/.</summary>
    public static TheoryData<string> RunConfigs => RepositoryPaths.RunConfigFiles().AsTheoryData();

    /// <summary>
    /// Every hand-written text file of the repository, whatever its format: sources, scenes, sidecars,
    /// locales, docs, configuration. The scope of the checks that look at the bytes of a file rather
    /// than at what is written in it.
    /// </summary>
    public static TheoryData<string> TextFiles => RepositoryPaths.TextFiles().AsTheoryData();

    /// <summary>
    /// One case per file, named by its repository-relative path — that name is what the test report
    /// shows.
    /// </summary>
    private static TheoryData<string> AsTheoryData(this IEnumerable<string> absolutePaths)
    {
        TheoryData<string> data = [];
        foreach (string path in absolutePaths)
        {
            data.Add(RepositoryPaths.Relative(path));
        }

        return data;
    }
}
