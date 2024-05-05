namespace NeonWarfare.Net;

public partial class ServerNetwork : AbstractNetwork
{
    
    public void Init()
    {
        base.Init();
        
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