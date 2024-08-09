using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using NeonWarfare.NetOld.Client;
using NeonWarfare.NetOld.Server;

namespace NeonWarfare.Net;


public static class ClientInitService
{
    
    [EventListener(ListenerSide.Client)]
    public static void OnConnectedToServerEvent(ConnectedToServerEvent connectedToServerEvent)
    {
        ClientRoot.Instance.GetWindow().MoveToForeground();
        ClientRoot.Instance.Game.ClearLoadingScreen(); //TODO в идеале вызывать только после синхронизации всех стартовых объектов (сервер должен отправить специальный пакет о том, что синхронизация закончена)
    }
}