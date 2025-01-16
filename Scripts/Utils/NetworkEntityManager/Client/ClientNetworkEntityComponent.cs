using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scripts.KludgeBox.Events;

namespace NeonWarfare.Scripts.Utils.NetworkEntityManager.Client;

public partial class ClientNetworkEntityComponent : NetworkEntityComponent
{
    public ClientNetworkEntityComponent(long nid) : base(nid) { }

    public override void _ExitTree() //TODO сделать так, чтобы не вызывалось при смене мира (World) целиком. Аналогично серверу.
    {
        ClientRoot.Instance.Game.World.NetworkEntityManager.RemoveEntity(this);
    }

    public void OnPositionEntityPacket(SC_PositionEntityPacket positionEntityPacket)
    {
        GetParent<Node2D>().Position = Vec(positionEntityPacket.X, positionEntityPacket.Y);
        GetParent<Node2D>().Rotation = positionEntityPacket.Dir;
    }
    
    public void OnDestroyEntityPacket(SC_DestroyEntityPacket destroyEntityPacket)
    {
        GetParent<Node>().QueueFree();
    }

    [EventListener(ListenerSide.Client)]
    public static void OnPositionEntityPacketListener(SC_PositionEntityPacket positionEntityPacket)
    {
        ClientNetworkEntityComponent entityComponent = ClientRoot.Instance.Game.World.NetworkEntityManager.GetEntityComponent(positionEntityPacket.Nid);
        entityComponent.OnPositionEntityPacket(positionEntityPacket);
    }
    
    [EventListener(ListenerSide.Client)]
    public static void OnDestroyEntityPacketListener(SC_DestroyEntityPacket destroyEntityPacket)
    {
        ClientNetworkEntityComponent entityComponent = ClientRoot.Instance.Game.World.NetworkEntityManager.GetEntityComponent(destroyEntityPacket.Nid);
        entityComponent.OnDestroyEntityPacket(destroyEntityPacket);
    }
}
