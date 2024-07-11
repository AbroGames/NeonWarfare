using System.Runtime.InteropServices.JavaScript;
using Godot;
using KludgeBox;

namespace NeonWarfare.Net;

public partial class ClientNetwork : AbstractNetwork
{
    
    public override void Init()
    {
        base.Init();
        
        Log.Info("Connect to Host");
        Error error = ENet.CreateHost(32);
        Log.Info(error);
        Log.Info(Api.HasMultiplayerPeer());
        Log.Info(Api.MultiplayerPeer);
        ENetPacketPeer peer = ENet.ConnectToHost("127.0.0.1", 25566);
        Log.Info(peer);
        Log.Info(Api.HasMultiplayerPeer());
        Log.Info(Api.MultiplayerPeer);
        
        //TODO Не забыть сделать Peer.Close после выхода с сервера
    }
}