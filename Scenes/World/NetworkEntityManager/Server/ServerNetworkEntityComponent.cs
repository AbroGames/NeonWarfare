using System;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using NeonWarfare;

public partial class ServerNetworkEntityComponent : NetworkEntityComponent
{
    public ServerNetworkEntityComponent(long nid) : base(nid) { }

    public override void _ExitTree() //TODO сделать так, чтобы не вызывалось при смене мира (World) целиком. Там отдельный один пакет будет.
    {
        ServerRoot.Instance.Game.World.NetworkEntityManager.RemoveEntity(this);
        Network.SendToAll(new ClientNetworkEntityComponent.SC_DestroyEntityPacket(Nid));
    }
}