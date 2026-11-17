using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Protocol;


namespace NeonWarfare.Scenes.World.Infra.ServerNetwork.Commands;

public interface IPlayerCommandHandler<TCommand> where TCommand : Command
{
    bool Validate(string senderUid, TCommand command);
    void Process(string senderUid, TCommand command);
}
