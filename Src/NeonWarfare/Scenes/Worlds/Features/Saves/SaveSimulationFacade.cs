using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Features.Chat;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scripts;
using NeonWarfare.Scenes.Worlds.Infra.Server.Saves;
using Serilog;

namespace NeonWarfare.Scenes.Worlds.Features.Saves;

[SimulationFacade]
public class SaveSimulationFacade(SaveService saveService, ChatSimulation chatSimulation, PlayerQuery players)
{
    private readonly ILogger _log = LogFactory.GetForStatic<SaveSimulationFacade>();

    public void Save(PlayerUid senderUid, string fileName)
    {
        PlayerModel sender = players.Get(senderUid);
        _log.Information("{nick} ({uid}) saves the game as '{fileName}'", sender.Nick, sender.Uid, fileName);
        
        // The reply comes at the end of the tick, and the sender may have left by then.
        saveService.RequestSave(fileName,
            () => chatSimulation.SendLocalizedMessageAsServerToPlayer(
                "HUD__CHAT_GAME_SAVED", [fileName], senderUid),
            _ => chatSimulation.SendLocalizedMessageAsServerToPlayer(
                "HUD__CHAT_GAME_SAVE_FAILED", [fileName], senderUid));
    }
}
