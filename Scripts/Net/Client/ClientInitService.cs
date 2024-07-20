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
    
    [EventListener]
    public static void OnConnectedToServerEvent(ConnectedToServerEvent connectedToServerEvent)
    {
        Root.Instance.GetWindow().MoveToForeground();
    }
}