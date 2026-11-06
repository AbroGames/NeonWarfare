using NeonWarfare.Scenes.World.Features.Players;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Commands;

namespace NeonWarfare.Scenes.World.Features.Saves;

// Only an admin is shown the save controls, so a refused command gets no reply
[CommandHandler]
public class SaveCommandHandler(SaveSimulationFacade saveSimulationFacade, PlayerQuery players)
    : IPlayerCommandHandler<SaveCommand>
{
    public bool Validate(string senderUid, SaveCommand command) =>
        players.Get(senderUid).IsAdmin && SaveFileNameRule.IsValid(command.FileName);

    public void Process(string senderUid, SaveCommand command) =>
        saveSimulationFacade.Save(senderUid, command.FileName);
}
