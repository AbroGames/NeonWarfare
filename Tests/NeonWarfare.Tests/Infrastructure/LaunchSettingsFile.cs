using System.Text.Json;

namespace NeonWarfare.Tests.Infrastructure;

/// <summary>
/// <c>Properties/launchSettings.json</c> read as an ordered list of run profiles. Only the shape this
/// repository actually uses is supported; anything else throws instead of being skipped, so a test can
/// never quietly pass on a file it misread.
/// </summary>
public sealed class LaunchSettingsFile
{
    /// <summary>
    /// Every profile starts the game the same way — the flag that points Godot at the project folder.
    /// The table in Docs/Quick-start.md leaves it out of the Arguments column on purpose, so it is
    /// stripped here rather than in the test.
    /// </summary>
    public const string ProjectPathArgument = "--path \"./\"";

    private const string ProfilesPropertyName = "profiles";

    private const string CommandLineArgsPropertyName = "commandLineArgs";

    private LaunchSettingsFile(string path, IReadOnlyList<LaunchProfile> profiles)
    {
        Path = path;
        Profiles = profiles;
    }

    /// <summary>Absolute path of the file.</summary>
    public string Path { get; }

    /// <summary>Repository-relative path, for failure messages.</summary>
    public string RelativePath => RepositoryPaths.Relative(Path);

    /// <summary>Every profile in file order — the order the doc table is checked against.</summary>
    public IReadOnlyList<LaunchProfile> Profiles { get; }

    /// <summary>The profile names, in file order.</summary>
    public IReadOnlyList<string> ProfileNames => Profiles.Select(profile => profile.Name).ToList();

    public static LaunchSettingsFile Load(string path)
    {
        string absolutePath = System.IO.Path.GetFullPath(path);
        string file = RepositoryPaths.Relative(absolutePath);
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(absolutePath));

        if (!document.RootElement.TryGetProperty(ProfilesPropertyName, out JsonElement profiles)
            || profiles.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"{file}: no '{ProfilesPropertyName}' object at the root. The file is the Rider run " +
                $"profiles, see Docs/Quick-start.md.");
        }

        List<LaunchProfile> parsed = [];
        foreach (JsonProperty profile in profiles.EnumerateObject())
        {
            if (profile.Value.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException($"{file}: profile '{profile.Name}' is not an object.");
            }

            if (!profile.Value.TryGetProperty(CommandLineArgsPropertyName, out JsonElement args)
                || args.ValueKind != JsonValueKind.String)
            {
                throw new InvalidOperationException(
                    $"{file}: profile '{profile.Name}' has no string '{CommandLineArgsPropertyName}'. " +
                    $"Every profile launches Godot with arguments — a profile without them cannot be " +
                    $"compared with the table in Docs/Quick-start.md.");
            }

            parsed.Add(new LaunchProfile(profile.Name, args.GetString()!));
        }

        return new LaunchSettingsFile(absolutePath, parsed);
    }

    /// <summary>The one launchSettings.json of the repository.</summary>
    public static LaunchSettingsFile Load() => Load(RepositoryPaths.LaunchSettingsPath);
}

/// <summary>One run profile: its name and the command line it starts Godot with.</summary>
public sealed record LaunchProfile(string Name, string CommandLineArgs)
{
    /// <summary>True when the profile passes the project folder to Godot, as all of them must.</summary>
    public bool HasProjectPath =>
        CommandLineArgs.StartsWith(LaunchSettingsFile.ProjectPathArgument, StringComparison.Ordinal);

    /// <summary>
    /// The command line as the Arguments column of Docs/Quick-start.md writes it: without the leading
    /// <see cref="LaunchSettingsFile.ProjectPathArgument"/>. Empty for a profile that adds nothing.
    /// </summary>
    public string ArgumentsWithoutProjectPath =>
        HasProjectPath
            ? CommandLineArgs[LaunchSettingsFile.ProjectPathArgument.Length..].Trim()
            : CommandLineArgs.Trim();
}
