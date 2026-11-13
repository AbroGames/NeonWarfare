using GdUnit4;
using Godot;
using NeonWarfare.GameTests.World.Fixtures;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Features.NewWorld;
using NeonWarfare.Scenes.World.Features.Players;
using NeonWarfare.Scenes.World.Infra.Entities;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.World.Features.NewWorld;

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
            new NetIdGenerator(), registry, new WorldRoot(root), TestWorldScenes.CreateCatalog(scenes));

        new NewWorldSimulationFacade(spawner).Create();

        AssertThat(root.GetChildren())
            .ContainsExactly(registry.GetSingle<PlayersStorage>(), registry.GetSingle<PlayersSessionStorage>());
    }
}
