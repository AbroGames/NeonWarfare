using System.Text.RegularExpressions;

namespace NeonWarfare.Tests.Infrastructure;

/// <summary>
/// <c>project.godot</c>, read as the two things the tests have questions about: the input actions the
/// engine knows and the <c>uid://</c> references the settings point at. The file is an INI, and nothing
/// in the build reads it — a renamed action or a deleted scene shows up only when the game runs.
/// </summary>
public sealed class GodotProjectFile
{
    /// <summary>An action of the input map: <c>KeyUp={</c>, with the event list on the lines below.</summary>
    private static readonly Regex ActionRegex =
        new(@"^(?<name>[A-Za-z_][A-Za-z0-9_]*)=\{", RegexOptions.Compiled);

    private static readonly Regex SectionRegex = new(@"^\[(?<name>\w+)\]", RegexOptions.Compiled);

    private static readonly Regex UidRegex = new(@"""(?<uid>uid://[^""]+)""", RegexOptions.Compiled);

    private const string InputSection = "input";

    private static readonly Lazy<GodotProjectFile> Instance = new(Load);

    private GodotProjectFile(IReadOnlyList<string> inputActions, IReadOnlyList<GodotUidReference> uidReferences)
    {
        InputActions = inputActions;
        UidReferences = uidReferences;
    }

    public static GodotProjectFile Current => Instance.Value;

    /// <summary>The actions declared in the <c>[input]</c> section, in file order.</summary>
    public IReadOnlyList<string> InputActions { get; }

    /// <summary>Every setting whose value is a <c>uid://</c>: the main scene, the icon, the theme.</summary>
    public IReadOnlyList<GodotUidReference> UidReferences { get; }

    private static GodotProjectFile Load()
    {
        string[] lines = TextFile.ReadLines(RepositoryPaths.ProjectSettingsPath);

        List<string> actions = [];
        List<GodotUidReference> references = [];
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

            foreach (Match uid in UidRegex.Matches(line))
            {
                // The setting the uid belongs to, for a message that says which one went stale.
                string setting = line.Split('=', 2)[0].Trim();
                references.Add(new GodotUidReference(setting, uid.Groups["uid"].Value, i + 1));
            }
        }

        return new GodotProjectFile(actions, references);
    }
}

/// <summary>A <c>uid://</c> written in project.godot: which setting holds it and on which line.</summary>
public sealed record GodotUidReference(string Setting, string Uid, int Line);
