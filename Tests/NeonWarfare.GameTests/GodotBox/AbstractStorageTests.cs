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
    public void TryGetScene_ReturnsFalseForUnknownName()
    {
        SceneStorageFixture storage = AutoFree(new SceneStorageFixture())!;

        storage._Ready();

        AssertThat(storage.TryGetScene("Unknown", out _)).IsFalse();
    }
}
