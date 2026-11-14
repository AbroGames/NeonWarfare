using GdUnit4;
using Godot;
using NeonWarfare.GameTests.World.Fixtures;
using NeonWarfare.GameTests.World.Infra.Protocol;
using NeonWarfare.Scenes.Game;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Infra.Entities;
using NeonWarfare.Scenes.World.Infra.Protocol;
using NeonWarfare.Scripts.GlobalServices;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Game;

[TestSuite]
public class GameProtocolTests
{
    // In the tree, as Game's child is when Game builds the protocol: the list is filled by the engine's _Ready
    [TestCase]
    [RequireGodotRuntime]
    public void Create_ReadyScenes_CatalogHasTheSceneKindsAndTheCodecItsHash()
    {
        WorldPackedScenes scenes = AutoFree(TestWorldScenes.CreateNotReady())!;
        ((SceneTree) Engine.GetMainLoop()).Root.AddChild(scenes);
        TypesMappingService mapping = NetMessageCodecTests.CreateMapping();

        (EntityCatalog entities, NetMessageCodec codec) = GameProtocol.Create(scenes, mapping);

        IReadOnlyList<PackedScene> list = scenes.GetScenesList();
        AssertThat(list).IsNotEmpty();
        for (int i = 0; i < list.Count; i++)
        {
            AssertThat(entities.GetKindId(list[i])).IsEqual(i);
        }
        AssertThat(codec.ProtocolHash).IsEqual(new NetMessageCodec(mapping, entities.Descriptors).ProtocolHash);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Create_ScenesBeforeReady_Throws()
    {
        WorldPackedScenes scenes = AutoFree(TestWorldScenes.CreateNotReady())!;

        AssertThrown(() => GameProtocol.Create(scenes, NetMessageCodecTests.CreateMapping()))
            .IsInstanceOf<InvalidOperationException>();
    }
}
