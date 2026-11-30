using GdUnit4;
using NeonWarfare.GameTests.Worlds.Fixtures;
using NeonWarfare.GameTests.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Game;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scripts.GlobalServices;
using RepliCAT;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Game;

[TestSuite]
public class GameProtocolTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void Create_Hash_IsTheHasherOverTheMappedTypesAndTheCatalogKinds()
    {
        WorldPackedScenes scenes = AutoFree(TestWorldScenes.Create())!;
        TypesMappingService mapping = NetMessageCodecTests.CreateMapping();

        GameProtocol protocol = GameProtocol.Create(scenes, mapping);

        ProtocolHash expected = new ProtocolHasher(new Replicator(mapping))
            .Compute(mapping.Types, TestWorldScenes.CreateCatalog(scenes).Descriptors);
        AssertThat(protocol.Hash).IsEqual(expected);
    }
}
