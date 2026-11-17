using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Peers;

namespace NeonWarfare.Scenes.World.Features.Players;

[CommandHandler]
public class PeerDisconnectedHandler(PlayerSimulationFacade playerSimulationFacade) : IPeerDisconnectedHandler
{
    public void Process(string uid) => playerSimulationFacade.Leave(uid);
}
