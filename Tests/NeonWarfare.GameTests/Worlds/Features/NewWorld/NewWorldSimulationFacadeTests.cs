using GdUnit4;
using Godot;
using NeonWarfare.GameTests.Worlds.Fixtures;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Features.NewWorld;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds.Features.NewWorld;

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
