using System;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using NeonWarfare;

namespace NeonWarfare.Scripts.Utils.NetworkEntityManager.Server;

public partial class ServerNetworkEntityComponent : NetworkEntityComponent
{
    public ServerNetworkEntityComponent(long nid) : base(nid) { }

    public override void _ExitTree()
    {
        //Проверка нужна, чтобы сообщения не отправлялись при смене мира целиком.
        //В случае полной смены мира будет новый NetworkEntityManager и попытка удаления вернёт false
        if (ServerRoot.Instance.Game.World.NetworkEntityManager.RemoveEntity(this))
        {
            Network.SendToAll(new ClientNetworkEntityComponent.SC_DestroyEntityPacket(Nid));
        }
    }
}
