using GdUnit4;
using Godot;
using KludgeBox.DI.Requests.NotNullCheck;
using NeonWarfare.GameTests.GodotBox.Fixtures;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.GodotBox;

[TestSuite]
public class CheckedAbstractStorageTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void Ready_ValidatesMembersThroughItsInjector()
    {
        CheckedStorageFixture storage = AutoFree(new CheckedStorageFixture())!;

        AssertThrown(() => storage._Ready()).IsInstanceOf<NotNullCheckFailedException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Ready_RegistersScenesOnceValidationPasses()
    {
        CheckedStorageFixture storage = AutoFree(new CheckedStorageFixture())!;
        PackedScene scene = new();
        storage.Required = scene;

        storage._Ready();

        AssertThat(storage.GetScenesDictionary()[nameof(CheckedStorageFixture.Required)]).IsSame(scene);
    }
}
