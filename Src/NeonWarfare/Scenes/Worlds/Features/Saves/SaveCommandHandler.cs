using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Server.Commands;

namespace NeonWarfare.Scenes.Worlds.Features.Saves;

/// <summary>A refused command gets no reply, because only an admin is shown the save controls.</summary>
[CommandHandler]
public class SaveCommandHandler(SaveSimulationFacade saveSimulationFacade, PlayerQuery players)
    : IPlayerCommandHandler<SaveCommand>
{
    public bool Validate(PlayerUid senderUid, SaveCommand command) =>
        players.Get(senderUid).IsAdmin && command.FileName.IsValid;

    public void Process(PlayerUid senderUid, SaveCommand command) =>
        saveSimulationFacade.Save(senderUid, command.FileName);
}
