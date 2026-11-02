using GdUnit4;
using Godot;
using NeonWarfare.GameTests.World.Fixtures;
using NeonWarfare.Scenes.World.Entities;
using NeonWarfare.Scenes.World.Entities.Storages;
using NeonWarfare.Scenes.World.ServerNetwork;
using NeonWarfare.Scenes.World.Simulations;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.World.Simulations;

[TestSuite]
public class NewWorldSimulationFacadeTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void Create_SpawnsBothStoragesUnderTheRoot()
    {
        Node root = AutoFree(new Node())!;
        WorldPackedScenes scenes = AutoFree(TestWorldScenes.Create())!;
        var registry = new EntityRegistry();
        var spawner = new EntitySpawner(
            new NetIdGenerator(), registry, new WorldRoot(root), scenes);

        new NewWorldSimulationFacade(spawner, scenes).Create();

        AssertThat(root.GetChildren())
            .ContainsExactly(registry.GetSingle<PersistenceStorage>(), registry.GetSingle<SessionStorage>());
    }
}
