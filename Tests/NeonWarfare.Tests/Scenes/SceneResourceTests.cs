using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Scenes;

/// <summary>
/// What the scene files say about the rest of the repository. Godot resolves every reference by its
/// <c>uid://</c> and treats the <c>path=</c> next to it as a hint, so a folder rename leaves the paths
/// pointing at files that no longer exist and the editor never says a word — the two Surface scenes had
/// been stale since the Surface → Surfaces rename.
/// </summary>
public class SceneResourceTests
{
    [Theory]
    [MemberData(nameof(GameFileSources.Resources), MemberType = typeof(GameFileSources))]
    public void ResourcePath_PointsToExistingFile(string relativePath)
    {
        SceneFile scene = SceneFile.Load(RepositoryPaths.Absolute(relativePath));
        FailureReport report = new($"{relativePath}: res:// paths that resolve to nothing");

        foreach (ExternalResource resource in scene.ExternalResources)
        {
            // res:// is the repository root. The check is case-sensitive on Linux, where the tests and
            // the CI run, so a path that only differs in case fails here as well.
            if (!File.Exists(RepositoryPaths.Absolute(resource.ResourcePath)))
            {
                report.Add($"line {resource.Line}: res://{resource.ResourcePath}");
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// "A scene and its handler live in the same folder under the same name" from Docs/Code-style.md.
    /// A scene whose root node carries no script at all is fine — Scenes/Entity/Walls/Wall.tscn is one.
    /// </summary>
    [Theory]
    [MemberData(nameof(GameFileSources.Scenes), MemberType = typeof(GameFileSources))]
    public void RootNodeScript_LivesNextToSceneWithSameName(string relativePath)
    {
        SceneFile scene = SceneFile.Load(RepositoryPaths.Absolute(relativePath));
        if (scene.RootScriptId is null)
        {
            return;
        }

        Assert.True(
            scene.RootScript is not null,
            $"{relativePath}: the root node's script is ExtResource(\"{scene.RootScriptId}\"), " +
            $"but no ext_resource declares that id");

        string expected = Path.ChangeExtension(relativePath, ".cs");
        Assert.True(
            string.Equals(scene.RootScript!.ResourcePath, expected, StringComparison.Ordinal),
            $"{relativePath}: the root node's script must be res://{expected}, " +
            $"found res://{scene.RootScript.ResourcePath}");
    }
}
