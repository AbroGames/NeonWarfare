using System;
using Godot;
using KludgeBox;

namespace NeonWarfare.Net;

public partial class ServerNetwork : AbstractNetwork
{
    
    public override void Init()
    {
        base.Init();
        
        Log.Info("Create Host");
        Error error = ENet.CreateHostBound("0.0.0.0", 25566, 32);
        //Peer.Crea
        Log.Info(error);
        Log.Info(Api.HasMultiplayerPeer());
        Log.Info(Api.MultiplayerPeer);
        
        /*
        Error error = Peer.CreateServer(port);
        if (error is Error.Ok)
        {
            PacketRegistry.IsSynchronized = true;
            Api.MultiplayerPeer = Peer;

            IsLocalServer = true;
            IsLocalClient = true;
            IsRemoteClient = true;

            Mode = NetworkMode.MultiplayerServer;
        }

        Log.Info($"({Mode}) Attempt to run multiplayer game finished with status: {error}");
        return error;*/
    }
}