using Godot;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Features.Chat;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Server.Events;
using NeonWarfare.Scenes.Worlds.Ports;
using Serilog;

namespace NeonWarfare.Scenes.Worlds.Features.Players;

[SimulationFacade]
public class PlayerSimulationFacade(
    ChatSimulation chat,
    EventOutbox outbox,
    PlayersStorageQuery players,
    PlayersSessionStorageQuery session,
    WorldAdmin admin,
    IServerOwner owner)
{
    private const string NewPlayerLog = "New player: {nick} ({uid})";
    private const string JoinedLog = "Player joined: {nick} ({uid})";
    private const string LeftLog = "Player left: {nick} ({uid})";
    private const string AdminGrantedLog = "Player is the admin of the world: {nick} ({uid})";
    private const string JoinedMessageKey = "HUD__CHAT_PLAYER_JOINED";
    private const string LeftMessageKey = "HUD__CHAT_PLAYER_LEFT";

    private readonly ILogger _log = LogFactory.GetForStatic<PlayerSimulationFacade>();

    // A returning player keeps the stored nick and color: they belong to the world, not to the client's settings.
    // The rights are only granted here, never taken: they may have been granted by another admin
    public void Join(string uid, string nick, Color color)
    {
        if (!players.Model.PlayerByUid.TryGetValue(uid, out PlayerModel player))
        {
            player = players.Model.AddPlayer(uid);
            player.Nick = nick;
            player.Color = color;
            _log.Information(NewPlayerLog, nick, uid);
        }

        if (uid == admin.Uid && !player.IsAdmin)
        {
            player.IsAdmin = true;
            _log.Information(AdminGrantedLog, player.Nick, uid);
        }

        session.Model.OnlinePlayerUids.Add(uid);
        _log.Information(JoinedLog, player.Nick, uid);
        chat.SendLocalizedMessageAsServerToAll(JoinedMessageKey, player.Nick);
        outbox.PublishToAll(new PlayerJoinedEvent(uid));
    }

    public void Leave(string uid)
    {
        PlayerModel player = players.Model.PlayerByUid[uid];
        session.Model.OnlinePlayerUids.Remove(uid);
        _log.Information(LeftLog, player.Nick, uid);
        chat.SendLocalizedMessageAsServerToAll(LeftMessageKey, player.Nick);
        if (uid == admin.Uid) owner.AdminLeft();
    }
}
