using NeonWarfare.Tests.Infrastructure;
using Xunit;

namespace NeonWarfare.Tests.Scenes;

/// <summary>
/// The other half of <see cref="SceneResourceTests"/>. That one checks the <c>path=</c> a human reads;
/// this one checks the <c>uid://</c> the engine actually loads by. The two can disagree — a file moved
/// outside the editor keeps its uid and loses its path, a file re-imported after a manual copy keeps its
/// path and gets a new uid — and in both cases the editor stays quiet, because it only ever needed one
/// of the two.
/// </summary>
public class UidReferenceTests
{
    /// <summary>
    /// A uid identifies a resource, so two files carrying the same one are the same resource as far as
    /// Godot is concerned: whichever it indexes last wins, and every reference to the other silently
    /// starts loading the wrong file. It happens by copying a file together with its .uid sidecar.
    /// </summary>
    [Fact]
    public void Uids_AreUniqueAcrossTheRepository()
    {
        FailureReport report = new("uid:// values claimed by more than one file");

        foreach ((string uid, IReadOnlyList<string> files) in UidIndex.Current.Duplicates)
        {
            report.Add($"{uid}: {string.Join(", ", files)}");
        }

        report.AssertEmpty();
    }

    [Theory]
    [MemberData(nameof(GameFileSources.Resources), MemberType = typeof(GameFileSources))]
    public void ExternalResourceUids_ResolveToAFile(string relativePath)
    {
        SceneFile scene = SceneFile.Load(RepositoryPaths.Absolute(relativePath));
        FailureReport report = new($"{relativePath}: uid:// references that resolve to nothing");

        foreach (ExternalResource resource in scene.ExternalResources)
        {
            // Reported rather than skipped: without a uid the reference is outside what this test can
            // check at all, and Godot has written one for every ext_resource since 4.4.
            if (resource.Uid is null)
            {
                report.Add($"line {resource.Line}: res://{resource.ResourcePath} has no uid=");
                continue;
            }

            if (UidIndex.Current.Resolve(resource.Uid) is null)
            {
                report.Add($"line {resource.Line}: {resource.Uid} (path says res://{resource.ResourcePath}) " +
                           $"— no file in the repository declares this uid");
            }
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// The uid and the path sitting on the same line must name one file. When they drift apart the game
    /// loads what the uid says while every reader of the repository — including the other test in this
    /// folder — believes the path.
    /// </summary>
    [Theory]
    [MemberData(nameof(GameFileSources.Resources), MemberType = typeof(GameFileSources))]
    public void ExternalResourceUidAndPath_NameTheSameFile(string relativePath)
    {
        SceneFile scene = SceneFile.Load(RepositoryPaths.Absolute(relativePath));
        FailureReport report = new($"{relativePath}: uid:// and path= that disagree");

        foreach (ExternalResource resource in scene.ExternalResources)
        {
            string? resolved = resource.Uid is null ? null : UidIndex.Current.Resolve(resource.Uid);
            if (resolved is null || string.Equals(resolved, resource.ResourcePath, StringComparison.Ordinal))
            {
                continue;
            }

            report.Add($"line {resource.Line}: {resource.Uid} is {resolved}, " +
                       $"but path says res://{resource.ResourcePath} — the game loads the first one");
        }

        report.AssertEmpty();
    }

    /// <summary>
    /// project.godot points at the main scene, the icon and the theme by uid and by nothing else, so
    /// there is no path next to them to notice going stale. A broken main_scene is a game that does not
    /// start.
    /// </summary>
    [Fact]
    public void ProjectSettingUids_ResolveToAFile()
    {
        FailureReport report = new("project.godot: uid:// settings that resolve to nothing");

        foreach (GodotUidReference reference in GodotProjectFile.Current.UidReferences)
        {
            if (UidIndex.Current.Resolve(reference.Uid) is null)
            {
                report.Add($"line {reference.Line}: {reference.Setting} = {reference.Uid}");
            }
        }

        report.AssertEmpty();
    }
}
