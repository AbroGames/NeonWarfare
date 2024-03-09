using System;
using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Net;

namespace NeoVector;

[GameService]
public class ClientService
{

    [EventListener]
    public void OnServerChangeWorldPacket(ServerChangeWorldPacket serverChangeWorldPacket)
    {
        Log.Warning("ServerChangeWorldPacket: " + serverChangeWorldPacket.WorldType);
    }
    
    [EventListener]
    public void OnServerSpawnPlayerPacket(ServerSpawnPlayerPacket serverSpawnPlayerPacket)
    {
        Log.Warning("ServerSpawnPlayerPacket: " + serverSpawnPlayerPacket.X);
    }
}