using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Events;

namespace NeonWarfare.Scripts.Utils.NetworkEntityManager.Client;

public partial class ClientNetworkEntityComponent : NetworkEntityComponent
{

    private long _lastOrderId = -1;
    
    public ClientNetworkEntityComponent(long nid) : base(nid) { }

    public override void _ExitTree() //TODO Попробовать сделать так, чтобы не вызывалось при смене игры или мира (Game/World) целиком. Аналогично сделать на сервере.
    {
        //Проверка нужна, чтобы при выходе в меню мы не получили NPE
        if (ClientRoot.Instance.Game != null)
        {
            ClientRoot.Instance.Game.World.NetworkEntityManager.RemoveEntity(this);
        }
    }

    public void OnPositionEntityPacket(SC_PositionEntityPacket positionEntityPacket)
    {
        if (positionEntityPacket.OrderId <= _lastOrderId) return;
        _lastOrderId = positionEntityPacket.OrderId;
        
        GetParent<Node2D>().Position = positionEntityPacket.Position;
        GetParent<Node2D>().Rotation = positionEntityPacket.Rotation;
    }
    
    public void OnDestroyEntityPacket(SC_DestroyEntityPacket destroyEntityPacket)
    {
        GetParent<Node>().QueueFree();
    }

    [EventListener(ListenerSide.Client)]
    public static void OnPositionEntityPacketListener(SC_PositionEntityPacket positionEntityPacket)
    {
        //Проверка нужна, т.к. у пакета Mode = Unreliable и из-за задержки может прийти после пакета смены мира.
        if (!ClientRoot.Instance.Game.World.NetworkEntityManager.HasEntityComponent(positionEntityPacket.Nid)) return;
        
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
