using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;


namespace NeonWarfare.Scenes.Worlds.Infra.Server.Commands;

public interface IPlayerCommandHandler<TCommand> where TCommand : Command
{
    bool Validate(string senderUid, TCommand command);
    void Process(string senderUid, TCommand command);
}
