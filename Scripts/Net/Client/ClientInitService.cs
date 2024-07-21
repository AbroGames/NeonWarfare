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
        
        var gameScene = ClientRoot.Instance.PackedScenes.Main.Game;
        ClientRoot.Instance.SetMainScene(gameScene.Instantiate<ClientGame>());
    }
}