using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Peers;

namespace NeonWarfare.Scenes.Worlds.Features.Players;

[CommandHandler]
public class PeerDisconnectedHandler(PlayerSimulationFacade playerSimulationFacade) : IPeerDisconnectedHandler
{
    public void Process(string uid) => playerSimulationFacade.Leave(uid);
}
