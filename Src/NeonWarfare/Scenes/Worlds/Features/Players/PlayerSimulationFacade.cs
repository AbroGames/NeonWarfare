using Godot;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Features.Chat;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scripts;
using NeonWarfare.Scenes.Worlds.Infra.Server.Events;
using NeonWarfare.Scenes.Worlds.Infra.Server.Peers;
using NeonWarfare.Scenes.Worlds.Ports;
using Serilog;

namespace NeonWarfare.Scenes.Worlds.Features.Players;

[SimulationFacade]
public class PlayerSimulationFacade(
    ChatSimulation chat,
    EventOutbox outbox,
    PlayerQuery players,
    WorldAdmin admin,
    IServerOwner owner)
{
    private readonly ILogger _log = LogFactory.GetForStatic<PlayerSimulationFacade>();

    /// <summary>
    /// A returning player keeps the stored nick and color, because they belong to the world, not to the client's
    /// settings.
    /// The admin rights are only granted here, never taken, because another admin may have granted them.
    /// </summary>
    public void Join(PlayerUid uid, string nick, Color color)
    {
        if (!players.PlayersModel.PlayerByUid.TryGetValue(uid, out PlayerModel player))
        {
            player = players.PlayersModel.AddPlayer(uid);
            player.Nick = nick;
            player.Color = color;
            _log.Information("New player: {nick} ({uid})", nick, uid);
        }

        if (uid == admin.Uid && !player.IsAdmin)
        {
            player.IsAdmin = true;
            _log.Information("Player is the admin of the world: {nick} ({uid})", player.Nick, uid);
        }

        players.OnlineModel.Uids.Add(uid);
        _log.Information("Player joined: {nick} ({uid})", player.Nick, uid);
        chat.SendLocalizedMessageAsServerToAll("HUD__CHAT_PLAYER_JOINED", [player.Nick]);
        outbox.PublishToAll(new PlayerJoinedEvent(uid));
    }

    /// <summary>
    /// A displaced admin is the same player on a new connection, so the server it started must not stop under it.
    /// </summary>
    public void Leave(PlayerUid uid, LeaveReason reason)
    {
        PlayerModel player = players.PlayersModel.PlayerByUid[uid];
        players.OnlineModel.Uids.Remove(uid);
        _log.Information("Player left: {nick} ({uid})", player.Nick, uid);
        chat.SendLocalizedMessageAsServerToAll("HUD__CHAT_PLAYER_LEFT", [player.Nick]);
        if (uid == admin.Uid && reason == LeaveReason.Disconnected) owner.AdminLeft();
    }
}
