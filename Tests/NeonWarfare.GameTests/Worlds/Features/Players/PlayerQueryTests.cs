using GdUnit4;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds.Features.Players;

[TestSuite]
public class PlayerQueryTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void PlayersAndOnline_AreTheModelsOfTheStorages()
    {
        var registry = new EntityRegistry();
        PlayersStorage players = AutoFree(new PlayersStorage())!;
        OnlinePlayersStorage onlinePlayers = AutoFree(new OnlinePlayersStorage())!;
        registry.Register(new NetId(1), players, 0);
        registry.Register(new NetId(2), onlinePlayers, 0);

        AssertThat(new PlayerQuery(registry).PlayersModel).IsSame(players.Model);
        AssertThat(new PlayerQuery(registry).OnlineModel).IsSame(onlinePlayers.Model);
    }

    // The registry's own message, so a read before the spawn says why the storage is missing
    [TestCase]
    [RequireGodotRuntime]
    public void PlayersAndOnline_BeforeTheSpawn_Throw()
    {
        var registry = new EntityRegistry();

        AssertThrown(() => _ = new PlayerQuery(registry).PlayersModel)
            .IsInstanceOf<InvalidOperationException>()
            .StartsWithMessage("Expected exactly one PlayersStorage, found 0.");
        AssertThrown(() => _ = new PlayerQuery(registry).OnlineModel)
            .IsInstanceOf<InvalidOperationException>()
            .StartsWithMessage("Expected exactly one OnlinePlayersStorage, found 0.");
    }
}
