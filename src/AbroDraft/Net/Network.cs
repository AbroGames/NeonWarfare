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
    /// <summary>
    /// Network is inactive.
    /// </summary>
    None = 0b000,
    
    /// <summary>
    /// This flag indicates if there is local client presented. Local client will receive direct methods calls instead of RPC.
    /// </summary>
    LocalClient = 0b001,

    /// <summary>
    /// This flag indicates if there is remote client presented. Remote clients will receive RPC instead of direct methods calls.
    /// </summary>
    RemoteClient = 0b010,
    
    /// <summary>
    /// This flag indicates if server is remote or local. There is must be no situation when both server sides is presented.
    /// </summary>
    LocalServer = 0b100
}

public partial class Network : Node
{
    public static Network Instance { get; private set; }
    public static MultiplayerApi Api { get; private set; }
    public static ENetMultiplayerPeer Peer { get; private set; }
    
    
    public static NetworkMode Mode { get; private set; } = NetworkMode.None;
    public static RemoteMode Remote { get; private set; }

    public static bool IsServer => IsLocalServer;
    public static bool IsClient => IsLocalClient;
    
    public static bool IsRemoteClient
    {
        get => Remote.HasFlag(RemoteMode.RemoteClient);
        set => Remote = value ? Remote | RemoteMode.RemoteClient : Remote & ~RemoteMode.RemoteClient;
    }

    public static bool IsLocalClient
    {
        get => Remote.HasFlag(RemoteMode.LocalClient);
        set => Remote = value ? Remote | RemoteMode.LocalClient : Remote & ~RemoteMode.LocalClient;
    }

    public static bool IsLocalServer
    {
        get => Remote.HasFlag(RemoteMode.LocalServer);
        set => Remote = value ? Remote | RemoteMode.LocalServer : Remote & ~RemoteMode.LocalServer;
    }
    
    public static bool IsRemoteServer
    {
        get => !IsLocalServer;
        set => IsLocalServer = !value;
    }

    public static bool BothLocal => IsLocalServer && IsLocalClient;

    public static long MyPeerId => Api.GetUniqueId();

    public static event Action<PacketSender, string> ReceivedRawPacket;
    public static event Action<PacketSender, AbstractPacket> ReceivedPacket;
    
    public Network()
    {
        Instance ??= this;
    }
    
    public override void _Ready()
    {
        Api = GetTree().GetMultiplayer();
        Peer = new ENetMultiplayerPeer();
        Api.PeerConnected += id =>
        {
            if (IsServer)
            {
                SendPacketToPeer(id, PacketRegistry.BuildSynchronizationPacket());
            }
        };
        ReceivedPacket += DefaultPacketProcessor;
    }

    public static Error CreateServer(int port)
    {
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
        return error;
    }
    
    public static Error ConnectToRemoteServer(string host, int port)
    {
        Error error = Peer.CreateClient(host, port);
        if (error is Error.Ok)
        {
            PacketRegistry.IsSynchronized = false;
            Api.MultiplayerPeer = Peer;
            
            IsLocalServer = false;
            IsLocalClient = true;
            IsRemoteClient = false;

            Mode = NetworkMode.ConnectedToRemote;
        }
        
        Log.Info($"({Mode}) Attempt to connect to remote server finished with status: {error}");
        return error;
    }

    public static void Disconnect()
    {
        IsLocalServer = false;
        IsLocalClient = false;
        IsRemoteClient = false;
        PacketRegistry.IsSynchronized = true;
        Peer.Close();
    }


    public static void SendPacketToClients(AbstractPacket packet)
    {
        var packetData = PacketConverter.Serialize(packet);
        
        if (IsServer)
        {
            Instance.Rpc(nameof(ReceivePacketFromServer), packetData);
        }

        if (IsClient)
        {
            Instance.ReceivePacketFromServer(packetData);
        }
    }

    public static void SendPacketToServer(AbstractPacket packet)
    {
        var packetData = PacketConverter.Serialize(packet);
        
        if (IsClient)
        {
            Instance.Rpc(nameof(ReceivePacketFromClient), packetData);
        }
        
        if (IsServer)
        {
            Instance.ReceivePacketFromClient(packetData);
        }
    }

    public static void SendPacketToPeer(long id, AbstractPacket packet)
    {
        if (!IsServer) throw new InvalidOperationException("Only server can send packets to specified peers");
        if (id == 1) throw new ArgumentException("Can't send packet from server to server");
        
        var packetData = PacketConverter.Serialize(packet);

        if (id == 0)
        {
            if (IsClient)
            {
                Instance.ReceivePacketFromServer(packetData);
                return;
            }
            throw new InvalidOperationException("Can't send packet to local client in dedicated server mode");
        }
        
        Instance.RpcId(id,nameof(ReceivePacketFromServer), packetData);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = TransferModeEnum.Unreliable, CallLocal = false)]
    internal void ReceivePacketFromClient(string packet)
    {
        var senderId = Api.GetRemoteSenderId();
        ReceivedRawPacket?.Invoke(senderId, packet);
        ReceivedPacket?.Invoke(senderId, PacketConverter.Deserialize(packet));
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = TransferModeEnum.Unreliable, CallLocal = false)]
    internal void ReceivePacketFromServer(string packet)
    {
        ReceivedRawPacket?.Invoke(1, packet);
        ReceivedPacket?.Invoke(1, PacketConverter.Deserialize(packet));
    }
    
    
    private static void DefaultPacketProcessor(PacketSender sender, AbstractPacket packet)
    {
        if (sender.IsServer && packet is RegistrySynchronizationPacket syncPacket)
        {
            Log.Info("Received a sync packet");
            PacketRegistry.SynchronizeRegistry(syncPacket);
            return;
        }
    }
}