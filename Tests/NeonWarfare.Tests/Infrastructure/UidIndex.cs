using System.Text.RegularExpressions;

namespace NeonWarfare.Tests.Infrastructure;

/// <summary>
/// Every <c>uid://</c> in the repository, mapped to the file it names. Godot resolves references by uid
/// and treats the <c>path=</c> written next to it as a hint, so this is the index the engine actually
/// works from — and the only way a test can tell a live reference from a rotten one.
/// <br/>
/// A uid is stored in one of three places, depending on what the file is: in a <c>.uid</c> sidecar for
/// code and shaders, in the header of a <c>.tscn</c> / <c>.tres</c>, and in the <c>.import</c> settings
/// for an asset Godot converts on load (textures, fonts).
/// </summary>
public sealed class UidIndex
{
    /// <summary>The uid line inside the <c>[remap]</c> section of an .import file.</summary>
    private static readonly Regex ImportUidRegex =
        new(@"^uid=""(?<uid>uid://[^""]+)""", RegexOptions.Compiled | RegexOptions.Multiline);

    private static readonly Lazy<UidIndex> Instance = new(Build);

    private readonly Dictionary<string, List<string>> _files;

    private UidIndex(Dictionary<string, List<string>> files)
    {
        _files = files;
    }

    /// <summary>Built once: the three sources add up to a few hundred small files.</summary>
    public static UidIndex Current => Instance.Value;

    /// <summary>Every uid found, with the repository-relative paths that claim it.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Entries =>
        _files.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlyList<string>)entry.Value,
            StringComparer.Ordinal);

    /// <summary>The uids claimed by more than one file — a copied sidecar makes two files one resource.</summary>
    public IEnumerable<KeyValuePair<string, IReadOnlyList<string>>> Duplicates =>
        Entries.Where(entry => entry.Value.Count > 1).OrderBy(entry => entry.Key, StringComparer.Ordinal);

    /// <summary>
    /// The repository-relative path a uid names, or <c>null</c> when nothing declares it. When several
    /// files claim the same uid the first one wins — <see cref="Duplicates"/> reports that separately.
    /// </summary>
    public string? Resolve(string uid) =>
        _files.TryGetValue(uid, out List<string>? files) ? files[0] : null;

    private static UidIndex Build()
    {
        Dictionary<string, List<string>> files = new(StringComparer.Ordinal);

        // A sidecar holds nothing but the uid, and Godot writes some of them with a UTF-8 BOM.
        // File.ReadAllText strips it; ReadAllBytes would not, and the BOM would end up glued to the
        // front of the uid, quietly resolving nothing.
        foreach (string sidecar in RepositoryPaths.UidFiles())
        {
            Add(files, File.ReadAllText(sidecar).Trim(), sidecar[..^".uid".Length]);
        }

        foreach (string resource in RepositoryPaths.ResourceFiles())
        {
            SceneFile scene = SceneFile.Load(resource);
            if (scene.Uid is not null)
            {
                Add(files, scene.Uid, resource);
            }
        }

        foreach (string import in RepositoryPaths.ImportFiles())
        {
            Match uid = ImportUidRegex.Match(File.ReadAllText(import));
            if (uid.Success)
            {
                Add(files, uid.Groups["uid"].Value, import[..^".import".Length]);
            }
        }

        return new UidIndex(files);
    }

    private static void Add(Dictionary<string, List<string>> files, string uid, string absolutePath)
    {
        if (uid.Length == 0)
        {
            return;
        }

        if (!files.TryGetValue(uid, out List<string>? claimants))
        {
            claimants = [];
            files[uid] = claimants;
        }

        claimants.Add(RepositoryPaths.Relative(absolutePath));
    }
}
