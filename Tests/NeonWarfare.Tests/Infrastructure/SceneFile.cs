using System.Text.RegularExpressions;

namespace NeonWarfare.Tests.Infrastructure;

/// <summary>
/// A Godot scene (<c>.tscn</c>) or resource (<c>.tres</c>), read as its external references plus the
/// script of the root node. Godot resolves references by <c>uid://</c>, so a <c>path=</c> left behind
/// by a rename keeps working in the editor and rots unnoticed — which is what
/// <see cref="ExternalResources"/> exists to catch.
/// </summary>
public sealed class SceneFile
{
    private static readonly Regex ExternalResourceRegex =
        new(@"^\[ext_resource\s+(?<attributes>.*)\]\s*$", RegexOptions.Compiled);

    // The lookbehind keeps id= from matching inside uid=, which sits on the very same line and would
    // otherwise hand back the uid as the ExtResource id.
    private static readonly Regex PathAttributeRegex =
        new(@"(?<!\w)path=""res://(?<path>[^""]+)""", RegexOptions.Compiled);

    private static readonly Regex IdAttributeRegex =
        new(@"(?<!\w)id=""(?<id>[^""]+)""", RegexOptions.Compiled);

    private static readonly Regex SectionRegex = new(@"^\[(?<kind>\w+)", RegexOptions.Compiled);

    private static readonly Regex RootScriptRegex =
        new(@"^script\s*=\s*ExtResource\(""(?<id>[^""]+)""\)", RegexOptions.Compiled);

    private SceneFile(string path, IReadOnlyList<ExternalResource> externalResources, string? rootScriptId)
    {
        Path = path;
        ExternalResources = externalResources;
        RootScriptId = rootScriptId;
        RootScript = externalResources.FirstOrDefault(resource => resource.Id == rootScriptId);
    }

    /// <summary>Absolute path of the file.</summary>
    public string Path { get; }

    /// <summary>Repository-relative path, for failure messages.</summary>
    public string RelativePath => RepositoryPaths.Relative(Path);

    /// <summary>Every <c>ext_resource</c> the file declares.</summary>
    public IReadOnlyList<ExternalResource> ExternalResources { get; }

    /// <summary>The ExtResource id the root node's script is assigned from, if it has one.</summary>
    public string? RootScriptId { get; }

    /// <summary>
    /// The root node's script. Null both for resources, which have no nodes, and for scenes whose root
    /// carries no script — a scene without a handler is allowed, see Docs/Code-style.md.
    /// </summary>
    public ExternalResource? RootScript { get; }

    public static SceneFile Load(string path)
    {
        string absolutePath = System.IO.Path.GetFullPath(path);
        string[] lines = File.ReadAllText(absolutePath).Split('\n');

        List<ExternalResource> resources = [];
        string? rootScriptId = null;
        int nodeSectionsSeen = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].TrimEnd('\r');

            Match section = SectionRegex.Match(line);
            if (section.Success && section.Groups["kind"].Value == "node")
            {
                nodeSectionsSeen++;
            }

            Match external = ExternalResourceRegex.Match(line);
            if (external.Success)
            {
                string attributes = external.Groups["attributes"].Value;
                Match path_ = PathAttributeRegex.Match(attributes);
                Match id = IdAttributeRegex.Match(attributes);
                if (path_.Success && id.Success)
                {
                    resources.Add(new ExternalResource(
                        path_.Groups["path"].Value, id.Groups["id"].Value, i + 1));
                }

                continue;
            }

            // The first [node ...] section is the root; a script line after any later section belongs
            // to a child node, which is allowed to live anywhere in the repository.
            if (nodeSectionsSeen == 1 && rootScriptId is null)
            {
                Match script = RootScriptRegex.Match(line);
                if (script.Success)
                {
                    rootScriptId = script.Groups["id"].Value;
                }
            }
        }

        return new SceneFile(absolutePath, resources, rootScriptId);
    }

    /// <summary>Every scene and resource that can hold a res:// reference.</summary>
    public static IReadOnlyList<SceneFile> LoadAll() =>
        RepositoryPaths.ResourceFiles().Select(Load).ToList();
}

/// <summary>
/// One <c>ext_resource</c>: the referenced file as a repository-relative path (the <c>res://</c>
/// prefix dropped, since res:// is the repository root), its ExtResource id and the line it is on.
/// </summary>
public sealed record ExternalResource(string ResourcePath, string Id, int Line);
