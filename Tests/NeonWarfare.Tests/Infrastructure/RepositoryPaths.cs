using System.Reflection;

namespace NeonWarfare.Tests.Infrastructure;

/// <summary>
/// Locations inside the repository the tests read from.
/// The root comes from the RepositoryRoot assembly metadata baked in by the .csproj, so it does not
/// depend on how deep the build output directory happens to be.
/// </summary>
public static class RepositoryPaths
{
    private const string RepositoryRootMetadataKey = "RepositoryRoot";

    /// <summary>
    /// Directories that hold nothing hand-written: build output, the engine cache, VCS and IDE state.
    /// They are skipped by name at any depth — bin/ and obj/ exist next to both .csproj files.
    /// </summary>
    private static readonly string[] GeneratedDirectories = ["bin", "obj", ".git", ".godot", ".idea", ".vs"];

    /// <summary>
    /// Extensions of every text file the repository owns — code, scenes, sidecars, locales, documentation
    /// and the build and tooling configuration. Assets/Fonts/*.license is text too but comes from
    /// outside and is left exactly as it was received, so its extension is not here.
    /// </summary>
    private static readonly HashSet<string> TextFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".uid", ".import", ".tscn", ".tres", ".gdshader", ".po", ".pot", ".md",
        ".csproj", ".sln", ".json", ".xml", ".yml", ".cfg", ".godot",
        ".editorconfig", ".gitattributes", ".gitignore",
    };

    public static string Root { get; } = ReadRoot();

    public static string ReadmePath { get; } = Path.Combine(Root, "README.md");

    public static string DocsDirectory { get; } = Path.Combine(Root, "Docs");

    /// <summary>
    /// The working notes kept next to the documentation. Unlike Docs/ it is not part of the repository
    /// contract and may simply not be there — every reader of it must handle its absence.
    /// </summary>
    public static string BrainDirectory { get; } = Path.Combine(Root, "brain");

    /// <summary>The two directories that hold every hand-written game source file.</summary>
    public static IReadOnlyList<string> SourceDirectories { get; } =
        [Path.Combine(Root, "Scenes"), Path.Combine(Root, "Scripts")];

    /// <summary>Everything that is neither a scene nor code — textures, fonts, shaders, locales.</summary>
    public static string AssetsDirectory { get; } = Path.Combine(Root, "Assets");

    public static string LocalesDirectory { get; } = Path.Combine(Root, "Assets", "Locales");

    /// <summary>The Godot project settings: the input map, the main scene, the icon and the theme.</summary>
    public static string ProjectSettingsPath { get; } = Path.Combine(Root, "project.godot");

    public static string GameProjectPath { get; } = Path.Combine(Root, "NeonWarfare.csproj");

    /// <summary>The test project root — Docs/Testing.md names its test classes relative to it.</summary>
    public static string TestsDirectory { get; } = Path.Combine(Root, "Tests", "NeonWarfare.Tests");

    public static string TestProjectPath { get; } =
        Path.Combine(TestsDirectory, "NeonWarfare.Tests.csproj");

    /// <summary>
    /// The smoke test project — a separate project that launches the engine, see Docs/Smoke-testing.md.
    /// Its scenarios are documented there rather than in Docs/Testing.md, so it is kept apart from
    /// <see cref="TestsDirectory"/> everywhere.
    /// </summary>
    public static string SmokeTestsDirectory { get; } = Path.Combine(Root, "Tests", "NeonWarfare.SmokeTests");

    public static string SmokeTestProjectPath { get; } =
        Path.Combine(SmokeTestsDirectory, "NeonWarfare.SmokeTests.csproj");

    /// <summary>The registry of all global services.</summary>
    public static string ServicesPath { get; } = Path.Combine(Root, "Scripts", "Services.cs");

    /// <summary>The world services — child nodes of World, one class per service.</summary>
    public static string WorldServiceDirectory { get; } = Path.Combine(Root, "Scenes", "World", "Service");

    /// <summary>The chat commands: ICommandProcessor and its implementations, one class per command.</summary>
    public static string CommandProcessorDirectory { get; } =
        Path.Combine(WorldServiceDirectory, "Command");

    /// <summary>The only place that declares a transfer channel — Consts.TransferChannel.</summary>
    public static string ConstsPath { get; } = Path.Combine(Root, "Scripts", "Consts.cs");

    /// <summary>The only place that names an input action.</summary>
    public static string InputActionsPath { get; } =
        Path.Combine(Root, "Scenes", "Entity", "Characters", "Controller", "Player", "Keys.cs");

    /// <summary>The localization template — the same keys as the .po files, with empty translations.</summary>
    public static string LocaleTemplatePath { get; } = Path.Combine(LocalesDirectory, "messages.pot");

    /// <summary>The only place allowed to declare command-line flags and parse them.</summary>
    public static string CmdArgsDirectory { get; } = Path.Combine(Root, "Scripts", "Content", "CmdArgs");

    /// <summary>The only place allowed to build a CmdArgsService and ask it for arguments.</summary>
    public static string RootStartersDirectory { get; } = Path.Combine(Root, "Scenes", "Root", "Starters");

    /// <summary>The run profiles Rider picks up by itself — see Docs/Quick-start.md.</summary>
    public static string LaunchSettingsPath { get; } = Path.Combine(Root, "Properties", "launchSettings.json");

    /// <summary>Rider Multi-Launch configurations, each starting several launch profiles at once.</summary>
    public static string RunConfigsDirectory { get; } = Path.Combine(Root, ".run");

    /// <summary>One file of <c>Docs/</c> by its file name — how every doc test names its document.</summary>
    public static string Doc(string fileName) => Path.Combine(DocsDirectory, fileName);

    /// <summary>All documentation files, sorted, as absolute paths.</summary>
    public static IReadOnlyList<string> DocFiles() =>
        Directory.GetFiles(DocsDirectory, "*.md", SearchOption.AllDirectories)
            .Select(Path.GetFullPath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

    /// <summary>Documentation files plus README.md — everything the doc tests scan.</summary>
    public static IReadOnlyList<string> DocFilesAndReadme() =>
        DocFiles().Prepend(Path.GetFullPath(ReadmePath)).ToList();

    /// <summary>
    /// Every game source file. Only Scenes/ and Scripts/ are scanned: the build output lives in bin/
    /// and obj/, and Tests/ has conventions of its own (see Docs/Testing.md).
    /// </summary>
    public static IReadOnlyList<string> SourceFiles() => Files(SourceDirectories, "*.cs");

    /// <summary>Every scene. Scenes only ever live under Scenes/.</summary>
    public static IReadOnlyList<string> SceneFiles() => Files([Path.Combine(Root, "Scenes")], "*.tscn");

    /// <summary>Scenes plus standalone resources — every file that can carry a res:// reference.</summary>
    public static IReadOnlyList<string> ResourceFiles() =>
        SceneFiles().Concat(Files([Path.Combine(Root, "Assets")], "*.tres"))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

    /// <summary>The translation files, without the template.</summary>
    public static IReadOnlyList<string> LocaleFiles() => Files([LocalesDirectory], "*.po");

    /// <summary>The translation files plus the template — the three files that must agree on keys.</summary>
    public static IReadOnlyList<string> LocaleFilesAndTemplate() =>
        LocaleFiles().Append(Path.GetFullPath(LocaleTemplatePath)).ToList();

    /// <summary>The files declaring the command-line arguments.</summary>
    public static IReadOnlyList<string> CmdArgsFiles() => Files([CmdArgsDirectory], "*.cs");

    /// <summary>Every Multi-Launch configuration in .run/.</summary>
    public static IReadOnlyList<string> RunConfigFiles() => Files([RunConfigsDirectory], "*.run.xml");

    /// <summary>Every hand-written test source file.</summary>
    public static IReadOnlyList<string> TestFiles() => HandWrittenSources(TestsDirectory);

    /// <summary>
    /// Every hand-written smoke test source file. Kept separate from <see cref="TestFiles"/>: the two
    /// projects are documented by two different files, and neither table may claim the other's tests.
    /// </summary>
    public static IReadOnlyList<string> SmokeTestFiles() => HandWrittenSources(SmokeTestsDirectory);

    /// <summary>
    /// Every hand-written .cs of the repository: the game, the tests and the smoke tests. The scope of
    /// the checks that are about how a C# file is written rather than about what the game does, and
    /// those hold for the test projects just as much.
    /// </summary>
    public static IReadOnlyList<string> CSharpFiles() =>
        SourceFiles().Concat(TestFiles()).Concat(SmokeTestFiles())
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

    /// <summary>The world service classes — every .cs under Scenes/World/Service, at any depth.</summary>
    public static IReadOnlyList<string> WorldServiceFiles() => Files([WorldServiceDirectory], "*.cs");

    /// <summary>The chat command classes — ICommandProcessor and every implementation of it.</summary>
    public static IReadOnlyList<string> CommandProcessorFiles() =>
        Files([CommandProcessorDirectory], "*.cs");

    /// <summary>
    /// Every <c>.uid</c> sidecar Godot keeps next to a file it cannot store a uid inside — a .cs or a
    /// .gdshader. Scenes and resources carry their uid in their own header instead.
    /// </summary>
    public static IReadOnlyList<string> UidFiles() =>
        Files(SourceDirectories.Append(AssetsDirectory), "*.uid");

    /// <summary>The import settings of the assets Godot converts on load — that is where their uid is.</summary>
    public static IReadOnlyList<string> ImportFiles() => Files([AssetsDirectory], "*.import");

    /// <summary>
    /// Every text file of the repository, whatever its format — the scope of the checks that look at
    /// the bytes of a file rather than at what is written in it. Unlike the other sources here this one
    /// walks the whole tree instead of naming directories, so a new folder is covered from the day it
    /// appears; what keeps the build output out is <see cref="GeneratedDirectories"/>.
    /// </summary>
    public static IReadOnlyList<string> TextFiles() =>
        AllFiles().Where(path => TextFileExtensions.Contains(Path.GetExtension(path))).ToList();

    /// <summary>
    /// Every file the repository owns, whatever its format — text and binary alike, with the same
    /// <see cref="GeneratedDirectories"/> left out. <see cref="TextFiles"/> is this list filtered by
    /// extension; this one is what "how large is the project" is counted from.
    /// </summary>
    public static IReadOnlyList<string> AllFiles() =>
        FilesUnder(Root).OrderBy(path => path, StringComparer.Ordinal).ToList();

    /// <summary>Every file of one directory and of everything below it, sorted, absolute.</summary>
    public static IReadOnlyList<string> AllFilesUnder(string directory) =>
        FilesUnder(directory).OrderBy(path => path, StringComparer.Ordinal).ToList();

    /// <summary>True when <paramref name="absolutePath"/> is inside <paramref name="directory"/>.</summary>
    public static bool IsInside(string absolutePath, string directory) =>
        Path.GetFullPath(absolutePath).StartsWith(
            Path.GetFullPath(directory) + Path.DirectorySeparatorChar,
            StringComparison.Ordinal);

    /// <summary>Repository-relative path with forward slashes, for readable failure messages.</summary>
    public static string Relative(string absolutePath) =>
        Path.GetRelativePath(Root, absolutePath).Replace(Path.DirectorySeparatorChar, '/');

    /// <summary>Resolves a path that a failure message reported back to an absolute one.</summary>
    public static string Absolute(string relativePath) =>
        Path.GetFullPath(Path.Combine(Root, relativePath));

    /// <summary>
    /// The <c>.cs</c> files of a project that somebody actually wrote. <c>bin/</c> and <c>obj/</c> are
    /// skipped: the build output holds generated sources — the xUnit entry point among them — that
    /// nobody wrote and nothing documents.
    /// </summary>
    private static IReadOnlyList<string> HandWrittenSources(string projectDirectory) =>
        Files([projectDirectory], "*.cs")
            .Where(path => !IsInside(path, Path.Combine(projectDirectory, "bin"))
                           && !IsInside(path, Path.Combine(projectDirectory, "obj")))
            .ToList();

    /// <summary>Files matching <paramref name="pattern"/> under the given roots, sorted, absolute.</summary>
    private static IReadOnlyList<string> Files(IEnumerable<string> directories, string pattern) =>
        directories
            .SelectMany(directory => Directory.GetFiles(directory, pattern, SearchOption.AllDirectories))
            .Select(Path.GetFullPath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

    /// <summary>Files of one directory and of everything below it, unsorted.</summary>
    private static IEnumerable<string> FilesUnder(string directory)
    {
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        foreach (string file in Directory.GetFiles(directory))
        {
            yield return Path.GetFullPath(file);
        }

        foreach (string child in Directory.GetDirectories(directory))
        {
            if (GeneratedDirectories.Contains(Path.GetFileName(child)))
            {
                continue;
            }

            foreach (string file in FilesUnder(child))
            {
                yield return file;
            }
        }
    }

    private static string ReadRoot()
    {
        string? value = typeof(RepositoryPaths).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == RepositoryRootMetadataKey)
            ?.Value;

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Assembly metadata '{RepositoryRootMetadataKey}' is missing. It is set by an " +
                $"AssemblyMetadata item in NeonWarfare.Tests.csproj — tests cannot locate the " +
                $"repository without it.");
        }

        return Path.GetFullPath(value);
    }
}
