using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Net;

namespace NeoVector;

[GameService]
public class WorldService
{

    [EventListener(ListenerSide.Server)]
    public void OnWorldPhysicsProcessEvent(WorldPhysicsProcessEvent worldPhysicsProcessEvent)
    {
        foreach (PlayerServerInfo playerServerInfo in Root.Instance.Server.PlayerServerInfo.Values)
        {
            Player player = playerServerInfo.Player;
            long nid = Root.Instance.NetworkEntityManager.GetNid(player);
            Network.SendPacketToClients(new ServerPositionEntityPacket(nid, player.Position.X, player.Position.Y, player.Rotation));
        }
    }
}