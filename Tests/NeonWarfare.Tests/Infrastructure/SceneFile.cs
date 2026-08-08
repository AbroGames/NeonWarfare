using System.Text.RegularExpressions;

namespace NeonWarfare.Tests.Infrastructure;

/// <summary>
/// A Godot scene (<c>.tscn</c>) or resource (<c>.tres</c>), read as its external references, its own
/// <c>uid://</c> and its node tree. Godot resolves references by <c>uid://</c>, so a <c>path=</c> left
/// behind by a rename keeps working in the editor and rots unnoticed — which is what
/// <see cref="ExternalResources"/> exists to catch.
/// </summary>
public sealed class SceneFile
{
    private static readonly Regex ExternalResourceRegex =
        new(@"^\[ext_resource\s+(?<attributes>.*)\]\s*$", RegexOptions.Compiled);

    private static readonly Regex NodeRegex = new(@"^\[node\s+(?<attributes>.*)\]\s*$", RegexOptions.Compiled);

    // The lookbehind keeps id= from matching inside uid=, which sits on the very same line and would
    // otherwise hand back the uid as the ExtResource id.
    private static readonly Regex PathAttributeRegex =
        new(@"(?<!\w)path=""res://(?<path>[^""]+)""", RegexOptions.Compiled);

    private static readonly Regex IdAttributeRegex =
        new(@"(?<!\w)id=""(?<id>[^""]+)""", RegexOptions.Compiled);

    private static readonly Regex UidAttributeRegex =
        new(@"(?<!\w)uid=""(?<uid>uid://[^""]+)""", RegexOptions.Compiled);

    private static readonly Regex NameAttributeRegex =
        new(@"(?<!\w)name=""(?<name>[^""]+)""", RegexOptions.Compiled);

    private static readonly Regex ParentAttributeRegex =
        new(@"(?<!\w)parent=""(?<parent>[^""]*)""", RegexOptions.Compiled);

    private static readonly Regex SectionRegex = new(@"^\[(?<kind>\w+)", RegexOptions.Compiled);

    private static readonly Regex ScriptRegex =
        new(@"^script\s*=\s*ExtResource\(""(?<id>[^""]+)""\)", RegexOptions.Compiled);

    /// <summary>The path a child of the root node writes in its <c>parent=</c> attribute.</summary>
    private const string RootNodePath = ".";

    private SceneFile(
        string path,
        string? uid,
        IReadOnlyList<ExternalResource> externalResources,
        IReadOnlyList<SceneNode> nodes)
    {
        Path = path;
        Uid = uid;
        ExternalResources = externalResources;
        Nodes = nodes;
        Root = nodes.FirstOrDefault(node => node.IsRoot);
        RootScriptId = Root?.ScriptId;
        RootScript = ScriptOf(Root);
    }

    /// <summary>Absolute path of the file.</summary>
    public string Path { get; }

    /// <summary>Repository-relative path, for failure messages.</summary>
    public string RelativePath => RepositoryPaths.Relative(Path);

    /// <summary>The file's own <c>uid://</c>, declared in its first line.</summary>
    public string? Uid { get; }

    /// <summary>Every <c>ext_resource</c> the file declares.</summary>
    public IReadOnlyList<ExternalResource> ExternalResources { get; }

    /// <summary>Every node the file declares, in file order. Empty for a resource, which has none.</summary>
    public IReadOnlyList<SceneNode> Nodes { get; }

    /// <summary>The root node, or <c>null</c> for a resource.</summary>
    public SceneNode? Root { get; }

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
        List<SceneNode> nodes = [];
        string? uid = null;
        SceneNode? current = null;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].TrimEnd('\r');

            Match external = ExternalResourceRegex.Match(line);
            if (external.Success)
            {
                current = null;
                string attributes = external.Groups["attributes"].Value;
                Match resourcePath = PathAttributeRegex.Match(attributes);
                Match id = IdAttributeRegex.Match(attributes);
                Match resourceUid = UidAttributeRegex.Match(attributes);
                if (resourcePath.Success && id.Success)
                {
                    resources.Add(new ExternalResource(
                        resourcePath.Groups["path"].Value,
                        id.Groups["id"].Value,
                        resourceUid.Success ? resourceUid.Groups["uid"].Value : null,
                        i + 1));
                }

                continue;
            }

            Match node = NodeRegex.Match(line);
            if (node.Success)
            {
                current = ReadNode(node.Groups["attributes"].Value, i + 1);
                if (current is not null)
                {
                    nodes.Add(current);
                }

                continue;
            }

            Match section = SectionRegex.Match(line);
            if (section.Success)
            {
                // The file's own uid lives in the [gd_scene ...] / [gd_resource ...] header.
                if (uid is null)
                {
                    Match headerUid = UidAttributeRegex.Match(line);
                    if (headerUid.Success)
                    {
                        uid = headerUid.Groups["uid"].Value;
                    }
                }

                current = null;
                continue;
            }

            if (current is not null && current.ScriptId is null)
            {
                Match script = ScriptRegex.Match(line);
                if (script.Success)
                {
                    current.ScriptId = script.Groups["id"].Value;
                }
            }
        }

        return new SceneFile(absolutePath, uid, resources, nodes);
    }

    /// <summary>Every scene and resource that can hold a res:// reference.</summary>
    public static IReadOnlyList<SceneFile> LoadAll() =>
        RepositoryPaths.ResourceFiles().Select(Load).ToList();

    /// <summary>
    /// Which scenes attach a given script, and to which node. Keyed by the script's repository-relative
    /// path, so a <c>.cs</c> file can be asked where in the tree it actually lives — the root node of its
    /// own scene, or a node deep inside somebody else's, the way LoadingAnimHandle.cs sits on
    /// Control/VBoxContainer/Anim/LoadingHandle in LoadingScreen.tscn.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<ScriptAttachment>> AttachmentsByScript()
    {
        Dictionary<string, List<ScriptAttachment>> attachments = new(StringComparer.Ordinal);

        foreach (SceneFile scene in LoadAll())
        {
            foreach (SceneNode node in scene.Nodes)
            {
                ExternalResource? script = scene.ScriptOf(node);
                if (script is null)
                {
                    continue;
                }

                if (!attachments.TryGetValue(script.ResourcePath, out List<ScriptAttachment>? list))
                {
                    list = [];
                    attachments[script.ResourcePath] = list;
                }

                list.Add(new ScriptAttachment(scene, node));
            }
        }

        return attachments.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlyList<ScriptAttachment>)entry.Value,
            StringComparer.Ordinal);
    }

    /// <summary>The <c>ext_resource</c> a node's script is assigned from, if it carries one.</summary>
    public ExternalResource? ScriptOf(SceneNode? node) =>
        node?.ScriptId is null
            ? null
            : ExternalResources.FirstOrDefault(resource => resource.Id == node.ScriptId);

    /// <summary>
    /// The direct children of <paramref name="node"/> — what a <c>[Child]</c> with <c>DeepScan</c> turned
    /// off can reach.
    /// </summary>
    public IEnumerable<SceneNode> Children(SceneNode node) =>
        Nodes.Where(candidate =>
            !candidate.IsRoot && string.Equals(candidate.ParentPath, node.NodePath, StringComparison.Ordinal));

    /// <summary>
    /// Everything below <paramref name="node"/>, at any depth. <c>[Child]</c> scans deep by default, so it
    /// matches a name anywhere in the subtree — Hud.ChatLabel lives inside a VBoxContainer.
    /// </summary>
    public IEnumerable<SceneNode> Descendants(SceneNode node)
    {
        if (node.IsRoot)
        {
            return Nodes.Where(candidate => !candidate.IsRoot);
        }

        string prefix = node.NodePath + "/";
        return Nodes.Where(candidate =>
            !candidate.IsRoot && candidate.NodePath.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static SceneNode? ReadNode(string attributes, int line)
    {
        Match name = NameAttributeRegex.Match(attributes);
        if (!name.Success)
        {
            return null;
        }

        string nodeName = name.Groups["name"].Value;

        Match parent = ParentAttributeRegex.Match(attributes);
        if (!parent.Success)
        {
            // No parent= at all: this is the root node, and Godot writes its path as "." everywhere else.
            return new SceneNode(nodeName, null, RootNodePath, line);
        }

        string parentPath = parent.Groups["parent"].Value;
        string nodePath = parentPath == RootNodePath ? nodeName : parentPath + "/" + nodeName;

        return new SceneNode(nodeName, parentPath, nodePath, line);
    }
}

/// <summary>
/// One <c>ext_resource</c>: the referenced file as a repository-relative path (the <c>res://</c>
/// prefix dropped, since res:// is the repository root), its ExtResource id, the <c>uid://</c> Godot
/// actually resolves by and the line it is on.
/// </summary>
public sealed record ExternalResource(string ResourcePath, string Id, string? Uid, int Line);

/// <summary>
/// One <c>[node ...]</c> section. <see cref="NodePath"/> is the path Godot writes in a child's
/// <c>parent=</c>: <c>"."</c> for the root, then <c>Control/VBoxContainer/Anim</c> and so on.
/// </summary>
public sealed class SceneNode
{
    public SceneNode(string name, string? parentPath, string nodePath, int line)
    {
        Name = name;
        ParentPath = parentPath;
        NodePath = nodePath;
        Line = line;
    }

    public string Name { get; }

    /// <summary>The <c>parent=</c> attribute, <c>null</c> for the root node.</summary>
    public string? ParentPath { get; }

    public string NodePath { get; }

    public int Line { get; }

    public bool IsRoot => ParentPath is null;

    /// <summary>The ExtResource id of the node's script, filled in while the section is being read.</summary>
    public string? ScriptId { get; internal set; }
}

/// <summary>A script found on a node: which scene, and which node inside it.</summary>
public sealed record ScriptAttachment(SceneFile Scene, SceneNode Node);
