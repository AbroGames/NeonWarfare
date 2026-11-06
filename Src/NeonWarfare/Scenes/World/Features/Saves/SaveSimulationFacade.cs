using System.Linq;
using Humanizer;
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
    private const string SavedReply = "The game is saved as '{0}'.";
    private const string FailedReply = "Saving the game as '{0}' failed.";

    private readonly ILogger _log = LogFactory.GetForStatic<SaveSimulationFacade>();

    public void Save(string senderUid, string fileName)
    {
        PlayerModel sender = players.Get(senderUid);
        _log.Information(SaveLog, sender.Nick, sender.Uid, fileName);
        saveService.RequestSave(fileName,
            () => Reply(SavedReply.FormatWith(fileName), senderUid),
            _ => Reply(FailedReply.FormatWith(fileName), senderUid));
    }

    // The reply comes at the end of the tick, and a leave queued after the command has run by then
    private void Reply(string text, string senderUid)
    {
        if (players.OnlinePlayers().All(player => player.Uid != senderUid)) return;

        chatSimulation.SendMessageAsServerToPlayer(text, senderUid);
    }
}
