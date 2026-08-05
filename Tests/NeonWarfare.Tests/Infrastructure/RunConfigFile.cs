using System.Xml.Linq;

namespace NeonWarfare.Tests.Infrastructure;

/// <summary>
/// A Rider Multi-Launch configuration from <c>.run/</c>, read as its name plus the launch profiles it
/// starts, in order. A profile is referenced by a plain string, so renaming it in
/// <c>Properties/launchSettings.json</c> leaves a configuration that Rider silently refuses to run.
/// Only the shape Rider writes for this project is supported; anything else throws.
/// </summary>
public sealed class RunConfigFile
{
    /// <summary>The suffix Rider gives its run configuration files.</summary>
    public const string FileSuffix = ".run.xml";

    /// <summary>
    /// How a Multi-Launch task names a launch profile:
    /// <c>runConfig:.NET Launch Settings Profile.NeonWarfare: Server</c>. The prefix is Rider's, the
    /// name after it is the .csproj name of the game project, and the part after ": " is the profile.
    /// </summary>
    private const string ProfileReferencePrefix = "runConfig:.NET Launch Settings Profile.NeonWarfare: ";

    private const string ConfigurationElementName = "configuration";

    private const string RowElementName = "ExecutableRowSnapshot";

    private const string OptionElementName = "option";

    private const string IdOptionName = "id";

    private RunConfigFile(string path, string name, IReadOnlyList<string> profileNames)
    {
        Path = path;
        Name = name;
        ProfileNames = profileNames;
    }

    /// <summary>Absolute path of the file.</summary>
    public string Path { get; }

    /// <summary>Repository-relative path, for failure messages.</summary>
    public string RelativePath => RepositoryPaths.Relative(Path);

    /// <summary>The configuration name Rider shows in the run widget.</summary>
    public string Name { get; }

    /// <summary>The file name without the <c>.run.xml</c> suffix.</summary>
    public string FileName => System.IO.Path.GetFileName(Path)[..^FileSuffix.Length];

    /// <summary>The launch profiles this configuration starts, in the order the file lists them.</summary>
    public IReadOnlyList<string> ProfileNames { get; }

    public static RunConfigFile Load(string path)
    {
        string absolutePath = System.IO.Path.GetFullPath(path);
        string file = RepositoryPaths.Relative(absolutePath);
        XDocument document = XDocument.Load(absolutePath);

        XElement[] configurations = document.Descendants(ConfigurationElementName)
            .Where(element => element.Attribute("name") is not null)
            .ToArray();
        if (configurations.Length != 1)
        {
            throw new InvalidOperationException(
                $"{file}: no single <{ConfigurationElementName} name=\"...\"> element. The file is a " +
                $"Rider Multi-Launch configuration, see Docs/Quick-start.md.");
        }

        XElement[] rows = configurations[0].Descendants(RowElementName).ToArray();
        if (rows.Length == 0)
        {
            throw new InvalidOperationException(
                $"{file}: no <{RowElementName}> tasks. A Multi-Launch that starts nothing is a broken " +
                $"file, not an empty one.");
        }

        List<string> profiles = [];
        foreach (XElement row in rows)
        {
            foreach (string value in ReferenceValues(row))
            {
                if (!value.StartsWith(ProfileReferencePrefix, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"{file}: task '{value}' does not start with '{ProfileReferencePrefix}'. " +
                        $"RunConfigFile only understands tasks that refer to a launch profile of the " +
                        $"game project — extend the parser before adding another kind.");
                }

                profiles.Add(value[ProfileReferencePrefix.Length..]);
            }
        }

        return new RunConfigFile(absolutePath, configurations[0].Attribute("name")!.Value, profiles);
    }

    /// <summary>Every Multi-Launch configuration in .run/, sorted by path.</summary>
    public static IReadOnlyList<RunConfigFile> LoadAll() =>
        RepositoryPaths.RunConfigFiles().Select(Load).ToList();

    /// <summary>
    /// The <c>id</c> options of one task. A row carries several <c>option</c> elements — the condition
    /// and the executable — and only the executable's id names a profile.
    /// </summary>
    private static IEnumerable<string> ReferenceValues(XElement row) =>
        row.Descendants(OptionElementName)
            .Where(option => option.Attribute("name")?.Value == IdOptionName)
            .Select(option => option.Attribute("value")?.Value ?? string.Empty);
}
