using GdUnit4;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds.Features.Players;

[TestSuite]
public class PlayersStorageQueryTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void Model_IsTheModelOfTheStorage()
    {
        var registry = new EntityRegistry();
        PlayersStorage players = AutoFree(new PlayersStorage())!;
        PlayersSessionStorage session = AutoFree(new PlayersSessionStorage())!;
        registry.Register(new NetId(1), players, 0);
        registry.Register(new NetId(2), session, 0);

        AssertThat(new PlayersStorageQuery(registry).Model).IsSame(players.Model);
        AssertThat(new PlayersSessionStorageQuery(registry).Model).IsSame(session.Model);
    }

    // The registry's own message, so a read before the spawn says why the storage is missing
    [TestCase]
    [RequireGodotRuntime]
    public void Model_BeforeTheSpawn_Throws()
    {
        var registry = new EntityRegistry();

        AssertThrown(() => _ = new PlayersStorageQuery(registry).Model)
            .IsInstanceOf<InvalidOperationException>()
            .StartsWithMessage("Expected exactly one PlayersStorage, found 0.");
        AssertThrown(() => _ = new PlayersSessionStorageQuery(registry).Model)
            .IsInstanceOf<InvalidOperationException>()
            .StartsWithMessage("Expected exactly one PlayersSessionStorage, found 0.");
    }
}
