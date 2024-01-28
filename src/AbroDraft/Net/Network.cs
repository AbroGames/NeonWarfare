using System;
using System.Net.Sockets;
using AbroDraft.Net.Packets;
using Godot;
using KludgeBox;
using static Godot.MultiplayerPeer;

namespace AbroDraft.Net;

public enum NetworkMode
{
    None,
    ConnectedToRemote,
    MultiplayerServer
}

[Flags]
public enum RemoteMode : byte
{
    None = 0b00,
    
    /// <summary>
    /// Represents the multiplayer side that is active in the local application. 
    /// If this flag is presented, then local methods will be executed directly.
    /// </summary>
    Local = 0b01,

    /// <summary>
    /// Represents the multiplayer side that is active in remote application(s). 
    /// If this flag is presented, then remote methods will be executed via RPC.
    /// </summary>
    Remote = 0b10,
    
    Both = 0b11
}

public partial class Network : Node
{
    public static Network Instance { get; private set; }
    public static MultiplayerApi Api { get; private set; }
    public static ENetMultiplayerPeer Peer { get; private set; }
    
    
    public static NetworkMode Mode { get; private set; } = NetworkMode.None;
    public static RemoteMode Remote { get; private set; }
    
    public static bool IsServer { get; private set; } = false;
    public static bool IsClient { get; private set; } = false;
    
    public static bool IsRemote
    {
        get => Remote.HasFlag(RemoteMode.Remote);
        set => Remote = value ? Remote | RemoteMode.Remote : Remote & ~RemoteMode.Remote;
    }

    public static bool IsLocal
    {
        get => Remote.HasFlag(RemoteMode.Local);
        set => Remote = value ? Remote | RemoteMode.Local : Remote & ~RemoteMode.Local;
    }

    public static event Action<string> ReceivedRawPacket;
    public static event Action<AbstractPacket> ReceivedPacket;
    
    public Network()
    {
        Instance ??= this;
    }
    
    public override void _Ready()
    {
        Api = GetTree().GetMultiplayer();
        Peer = new ENetMultiplayerPeer();
    }

    public static Error CreateServer(int port)
    {
        Error error = Peer.CreateServer(port);
        if (error is Error.Ok)
        {
            Api.MultiplayerPeer = Peer;
            
            IsServer = true;
            IsClient = true;

            Mode = NetworkMode.MultiplayerServer;
        }

        Log.Info($"({Mode}) Attempt to run multiplayer game finished with status: {error}");
        return error;
    }
    
    public static Error ConnectToRemoteServer(string host, int port)
    {
        Error error = Peer.CreateClient(host, port);
        if (error is Error.Ok)
        {
            Api.MultiplayerPeer = Peer;
            
            IsServer = false;
            IsClient = true;

            Mode = NetworkMode.ConnectedToRemote;
        }
        
        Log.Info($"({Mode}) Attempt to connect to remote server finished with status: {error}");
        return error;
    }


    public static void SendPacket(AbstractPacket packet)
    {
        var packetData = PacketConverter.Serialize(packet);
        
        if (IsServer)
        {
            Instance.Rpc(nameof(ReceivePacketFromServer), packetData);
            Instance.ReceivePacketFromServer(packetData);
        }

        if (IsClient)
        {
            Instance.Rpc(nameof(ReceivePacketFromClient), packetData);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = TransferModeEnum.Unreliable, CallLocal = false)]
    internal void ReceivePacketFromClient(string packet)
    {
        ReceivedRawPacket?.Invoke(packet);
        ReceivedPacket.Invoke(PacketConverter.Deserialize(packet));
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = TransferModeEnum.Unreliable, CallLocal = false)]
    internal void ReceivePacketFromServer(string packet)
    {
        ReceivedRawPacket?.Invoke(packet);
        ReceivedPacket.Invoke(PacketConverter.Deserialize(packet));
    }
}