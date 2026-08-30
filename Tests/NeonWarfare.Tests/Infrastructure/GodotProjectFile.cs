using System.Text.RegularExpressions;

namespace NeonWarfare.Tests.Infrastructure;

/// <summary>
/// <c>project.godot</c>, read as the things the tests have questions about: the input actions the
/// engine knows and the <c>uid://</c> and <c>res://</c> references the settings point at. The file is an
/// INI, and nothing in the build reads it — a renamed action or a deleted scene shows up only when the
/// game runs.
/// </summary>
public sealed class GodotProjectFile
{
    /// <summary>An action of the input map: <c>KeyUp={</c>, with the event list on the lines below.</summary>
    private static readonly Regex ActionRegex =
        new(@"^(?<name>[A-Za-z_][A-Za-z0-9_]*)=\{", RegexOptions.Compiled);

    private static readonly Regex SectionRegex = new(@"^\[(?<name>\w+)\]", RegexOptions.Compiled);

    private static readonly Regex UidRegex = new(@"""(?<uid>uid://[^""]+)""", RegexOptions.Compiled);

    private static readonly Regex ResPathRegex = new(@"""res://(?<path>[^""]+)""", RegexOptions.Compiled);

    private const string InputSection = "input";

    private static readonly Lazy<GodotProjectFile> Instance = new(Load);

    private GodotProjectFile(
        IReadOnlyList<string> inputActions,
        IReadOnlyList<GodotUidReference> uidReferences,
        IReadOnlyList<GodotPathReference> pathReferences)
    {
        InputActions = inputActions;
        UidReferences = uidReferences;
        PathReferences = pathReferences;
    }

    public static GodotProjectFile Current => Instance.Value;

    /// <summary>The actions declared in the <c>[input]</c> section, in file order.</summary>
    public IReadOnlyList<string> InputActions { get; }

    /// <summary>Every setting whose value is a <c>uid://</c>: the main scene, the icon, the theme.</summary>
    public IReadOnlyList<GodotUidReference> UidReferences { get; }

    /// <summary>
    /// Every <c>res://</c> in a setting's value, one per element of an array such as
    /// <c>locale/translations</c>.
    /// </summary>
    public IReadOnlyList<GodotPathReference> PathReferences { get; }

    private static GodotProjectFile Load()
    {
        string[] lines = TextFile.ReadLines(RepositoryPaths.ProjectSettingsPath);

        List<string> actions = [];
        List<GodotUidReference> references = [];
        List<GodotPathReference> paths = [];
        string section = string.Empty;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            Match sectionMatch = SectionRegex.Match(line);
            if (sectionMatch.Success)
            {
                section = sectionMatch.Groups["name"].Value;
                continue;
            }

            if (section == InputSection)
            {
                Match action = ActionRegex.Match(line);
                if (action.Success)
                {
                    actions.Add(action.Groups["name"].Value);
                }
            }

            // The setting a reference belongs to, for a message that says which one went stale.
            string setting = line.Split('=', 2)[0].Trim();

            foreach (Match uid in UidRegex.Matches(line))
            {
                references.Add(new GodotUidReference(setting, uid.Groups["uid"].Value, i + 1));
            }

            foreach (Match path in ResPathRegex.Matches(line))
            {
                paths.Add(new GodotPathReference(setting, path.Groups["path"].Value, i + 1));
            }
        }

        return new GodotProjectFile(actions, references, paths);
    }
}

/// <summary>A <c>uid://</c> written in project.godot: which setting holds it and on which line.</summary>
public sealed record GodotUidReference(string Setting, string Uid, int Line);

/// <summary>
/// A <c>res://</c> written in project.godot: which setting holds it, the file as a repository-relative
/// path (res:// is the repository root) and the line it is on.
/// </summary>
public sealed record GodotPathReference(string Setting, string ResourcePath, int Line);
