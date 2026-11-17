using KludgeBox.Logging;
using NeonWarfare.Scenes.World.Features.Chat;
using NeonWarfare.Scenes.World.Features.Players;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Saves;
using Serilog;

namespace NeonWarfare.Scenes.World.Features.Saves;

[SimulationFacade]
public class SaveSimulationFacade(SaveService saveService, ChatSimulation chatSimulation, PlayerQuery players)
{
    private const string SaveLog = "{nick} ({uid}) saves the game as '{fileName}'";
    private const string SavedMessageKey = "HUD__CHAT_GAME_SAVED";
    private const string SaveFailedMessageKey = "HUD__CHAT_GAME_SAVE_FAILED";

    private readonly ILogger _log = LogFactory.GetForStatic<SaveSimulationFacade>();

    public void Save(string senderUid, string fileName)
    {
        PlayerModel sender = players.Get(senderUid);
        _log.Information(SaveLog, sender.Nick, sender.Uid, fileName);
        
        // The reply comes at the end of the tick and is not guaranteed: the sender may have left by then
        saveService.RequestSave(fileName,
            () => chatSimulation.SendLocalizedMessageAsServerToPlayer(senderUid, SavedMessageKey, fileName),
            _ => chatSimulation.SendLocalizedMessageAsServerToPlayer(senderUid, SaveFailedMessageKey, fileName));
    }
}
