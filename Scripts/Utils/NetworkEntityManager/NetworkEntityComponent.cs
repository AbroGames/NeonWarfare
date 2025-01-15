using System;
using Godot;
using KludgeBox.Events;
using NeonWarfare;

public partial class NetworkEntityComponent : Node
{
    public long Nid { get; set; } = -1; 
    public Action BeforeDestroy { get; set; }
    
    public NetworkEntityComponent(long nid)
    {
        Nid = nid;
    }

    public void OnPositionEntityPacket(SC_PositionEntityPacket positionEntityPacket)
    {
        GetParent<Node2D>().Position = Vec(positionEntityPacket.X, positionEntityPacket.Y);
        GetParent<Node2D>().Rotation = positionEntityPacket.Dir;
    }
    
    public void OnDestroyEntityPacket(SC_DestroyEntityPacket destroyEntityPacket)
    {
        BeforeDestroy.Invoke();
        GetParent<Node>().QueueFree();
    }

    [EventListener(ListenerSide.Client)]
    public static void OnPositionEntityPacketListener(SC_PositionEntityPacket positionEntityPacket)
    {
        NetworkEntityComponent entityComponent = ClientRoot.Instance.Game.World.NetworkEntityManager.GetEntityComponent(positionEntityPacket.Nid);
        entityComponent.OnPositionEntityPacket(positionEntityPacket);
    }
    
    [EventListener(ListenerSide.Client)]
    public static void OnDestroyEntityPacketListener(SC_DestroyEntityPacket destroyEntityPacket)
    {
        NetworkEntityComponent entityComponent = ClientRoot.Instance.Game.World.NetworkEntityManager.GetEntityComponent(destroyEntityPacket.Nid);
        entityComponent.OnDestroyEntityPacket(destroyEntityPacket);
    }
}