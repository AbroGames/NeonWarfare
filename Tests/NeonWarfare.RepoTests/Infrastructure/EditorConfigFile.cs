using System.Text.RegularExpressions;

namespace NeonWarfare.RepoTests.Infrastructure;

/// <summary>
/// The root <c>.editorconfig</c>, read as a map "section → key → value". A test enforcing one of its rules
/// takes the value from here rather than repeating it as a constant, so the two cannot drift apart.
/// Only exact section names are looked up: glob matching is the editor's business, not the tests'.
/// </summary>
public sealed class EditorConfigFile
{
    private static readonly Regex SectionRegex = new(@"^\[(?<name>.+)\]$", RegexOptions.Compiled);

    private static readonly Regex PropertyRegex =
        new(@"^(?<key>[^=]+?)\s*=\s*(?<value>.*)$", RegexOptions.Compiled);

    private static readonly Lazy<EditorConfigFile> Instance = new(Load);

    private readonly Dictionary<string, Dictionary<string, string>> _sections;

    private EditorConfigFile(Dictionary<string, Dictionary<string, string>> sections)
    {
        _sections = sections;
    }

    public static EditorConfigFile Current => Instance.Value;

    /// <summary>The integer value of <paramref name="key"/> in <c>[section]</c>; throws when it is absent.</summary>
    public int IntValue(string section, string key)
    {
        if (!_sections.TryGetValue(section, out Dictionary<string, string>? properties)
            || !properties.TryGetValue(key, out string? value))
        {
            throw new InvalidOperationException($".editorconfig: [{section}] has no {key}");
        }

        return int.Parse(value);
    }

    private static EditorConfigFile Load()
    {
        Dictionary<string, Dictionary<string, string>> sections = [];
        Dictionary<string, string>? current = null;

        foreach (string rawLine in TextFile.ReadLines(RepositoryPaths.EditorConfigPath))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith(';'))
            {
                continue;
            }

            Match section = SectionRegex.Match(line);
            if (section.Success)
            {
                current = [];
                sections[section.Groups["name"].Value] = current;
                continue;
            }

            Match property = PropertyRegex.Match(line);
            if (property.Success && current != null)
            {
                current[property.Groups["key"].Value] = property.Groups["value"].Value;
            }
        }

        return new EditorConfigFile(sections);
    }
}
