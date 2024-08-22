using Godot;
using KludgeBox.Events;
using NeonWarfare.NetOld.Server;

namespace NeonWarfare;

public partial class NetworkEntity : Node2D
{
    /* TODO
    [EventListener(ListenerSide.Client)]
    public static void OnServerPositionEntityPacket(ServerPositionEntityPacket serverPositionEntityPacket)
    {
        NetworkEntity entity = ClientRoot.Instance.Game.World.NetworkEntityManager.GetNode<NetworkEntity>(serverPositionEntityPacket.Nid);
        entity.Position = Vec(serverPositionEntityPacket.X, serverPositionEntityPacket.Y);
        entity.Rotation = serverPositionEntityPacket.Dir;
    }

    [EventListener(ListenerSide.Client)]
    public static void OnServerDestroyEntityPacket(ServerDestroyEntityPacket serverDestroyEntityPacket)
    {
        NetworkEntity entity = ClientRoot.Instance.Game.World.NetworkEntityManager.RemoveEntity<NetworkEntity>(serverDestroyEntityPacket.Nid);
        entity.QueueFree();
    }*/
}