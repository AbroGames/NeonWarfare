using GdUnit4;
using Godot;
using NeonWarfare.GameTests.GodotBox.Fixtures;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.GodotBox;

[TestSuite]
public class AbstractStorageTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void Ready_RegistersExportedPackedSceneProperties()
    {
        SceneStorageFixture storage = AutoFree(new SceneStorageFixture())!;
        PackedScene first = new();
        PackedScene second = new();
        storage.First = first;
        storage.Second = second;

        storage._Ready();

        AssertThat(storage.TryGetScene(nameof(SceneStorageFixture.First), out PackedScene registered)).IsTrue();
        AssertThat(registered).IsSame(first);
        AssertThat(storage.GetScenesDictionary()[nameof(SceneStorageFixture.Second)]).IsSame(second);
        AssertThat(storage.GetScenesList().Contains(first)).IsTrue();
        AssertThat(storage.GetScenesList().Contains(second)).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Ready_SkipsNonExportedPropertiesAndFields()
    {
        SceneStorageFixture storage = AutoFree(new SceneStorageFixture())!;

        storage._Ready();

        AssertThat(storage.GetScenesDictionary().ContainsKey(nameof(SceneStorageFixture.NotExported))).IsFalse();
        AssertThat(storage.GetScenesDictionary().ContainsKey(nameof(SceneStorageFixture.ExportedField))).IsFalse();
        AssertThat(storage.GetScenesDictionary().Count).IsEqual(3);
        AssertThat(storage.GetScenesList().Count).IsEqual(3);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Ready_RunsPreReadyBeforeRegistering()
    {
        SceneStorageFixture storage = AutoFree(new SceneStorageFixture())!;
        PackedScene scene = new();
        storage.NotExported = scene;

        storage._Ready();

        AssertThat(storage.GetScenesDictionary()[nameof(SceneStorageFixture.AssignedInPreReady)]).IsSame(scene);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void GetSceneId_IsTheIndexInTheList_UnknownSceneThrows()
    {
        SceneStorageFixture storage = AutoFree(new SceneStorageFixture())!;
        storage.First = new PackedScene();
        storage.Second = new PackedScene();
        storage._Ready();
        IReadOnlyList<PackedScene> scenes = storage.GetScenesList();

        AssertThat(storage.GetSceneId(storage.First)).IsEqual(IndexOf(scenes, storage.First));
        AssertThat(storage.GetSceneId(storage.Second)).IsEqual(IndexOf(scenes, storage.Second));
        AssertThrown(() => storage.GetSceneId(new PackedScene())).IsInstanceOf<ArgumentException>();
        AssertThrown(() => storage.GetSceneId(null!)).IsInstanceOf<ArgumentException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void TryGetScene_ReturnsFalseForUnknownName()
    {
        SceneStorageFixture storage = AutoFree(new SceneStorageFixture())!;

        storage._Ready();

        AssertThat(storage.TryGetScene("Unknown", out _)).IsFalse();
    }

    private static int IndexOf(IReadOnlyList<PackedScene> scenes, PackedScene scene) =>
        scenes.Select((candidate, index) => (candidate, index)).Single(pair => pair.candidate == scene).index;
}
