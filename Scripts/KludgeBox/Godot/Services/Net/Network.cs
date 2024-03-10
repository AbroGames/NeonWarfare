using System;
using System.Collections.Generic;
using Godot;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Net.Packets;
using NeoVector;
using static Godot.MultiplayerPeer;

namespace KludgeBox.Net;

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
    private static Network Instance
    {
        get
        {
            if (_instance == null)
            {
                // Check if we were loaded via Autoload
                _instance = ((SceneTree)Engine.GetMainLoop()).Root.GetNodeOrNull<Network>(typeof(Network).Name);
                if (_instance == null)
                {
                    // Instantiate to root at runtime
                    _instance = new Network();
                    _instance.Name = typeof(Network).Name;
                    _instance.InitGlobalInstance();
                }
            }
            return _instance;
        }
    }

    static Network()
    {
        var instance = Instance;
    }

    public static void Init()
    {
        ReceivedPacket += EventBus.Publish;
        //TODO ReceivedRawPacket += (sender, packet) => Log.Debug($"Received packet from {sender.Id}: {packet}");
    }
    
    private void InitGlobalInstance()
    {
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(this);
        
        Api = ((SceneTree)Engine.GetMainLoop()).GetMultiplayer();
        Peer = new ENetMultiplayerPeer();
        Api.PeerConnected += id =>
        {
            if (IsServer)
            {
                SendPacketToPeer(id, PacketRegistry.BuildSynchronizationPacket());
            }
        };
        ReceivedPacket += DefaultPacketProcessor;

        Api.PeerConnected += id => EventBus.Publish<IEvent>(IsServer ? new PeerConnectedServerEvent(id) : new PeerConnectedClientEvent(id));
        Api.PeerDisconnected += id => EventBus.Publish<IEvent>(IsServer ? new PeerDisconnectedServerEvent(id) : new PeerDisconnectedClientEvent(id));
        Api.ConnectedToServer += () => EventBus.Publish<IEvent>(new ConnectedToServerEvent());
        Api.ConnectionFailed += () => EventBus.Publish<IEvent>(new ConnectionToServerFailedEvent());
        Api.ServerDisconnected += () => EventBus.Publish<IEvent>(new ServerDisconnectedEvent());
    }
	
    private static Network _instance;

    public static List<Type> PacketsNotLogged = [typeof(ServerPositionEntityPacket), typeof(ClientMovementPlayerPacket)];
    
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

    /// <summary>
    /// Actual network peer ID. Will be 1 if you are hosting server.
    /// </summary>
    public static long MyPeerId => Api.GetUniqueId();
    
    /// <summary>
    /// Virtual network peer ID. Will be 0 if you are hosting server.
    /// </summary>
    public static long MyPlayerId => IsServer ? 0 : MyPeerId;

    public static event Action<PacketSender, string> ReceivedRawPacket;
    public static event Action<AbstractPacket> ReceivedPacket;
    
    
    public override void _Ready()
    {
        
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
    
    public static Error CreateDedicatedServer(int port)
    {
        Error error = Peer.CreateServer(port);
        if (error is Error.Ok)
        {
            PacketRegistry.IsSynchronized = true;
            Api.MultiplayerPeer = Peer;
            
            IsLocalServer = true;
            IsLocalClient = false;
            IsRemoteClient = false;

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

    public static void SendPacketsToClients(PacketBuffer buffer)
    {
        foreach (AbstractPacket packet in buffer.EnumeratePackets())
        {
            SendPacketToClients(packet);
        }
    }

    public static void SendPacketsToServer(PacketBuffer buffer)
    {
        foreach (AbstractPacket packet in buffer.EnumeratePackets())
        {
            SendPacketToClients(packet);
        }
    }

    public static void SendPacketToClients(AbstractPacket packet)
    {
        var packetData = PacketConverter.Serialize(packet);

        if (!PacketsNotLogged.Contains(packet.GetType()))
        {
            Log.Debug($"SendPacketToClients: {packetData}");
        }

        if (IsServer)
        {
            if (packet.IsReliable)
            {
                Instance.Rpc(nameof(ReceivePacketFromServer), packetData);
            }
            else
            {
                Instance.Rpc(nameof(ReceivePacketFromServerUnreliable), packetData);
            }
        }

        if (IsClient)
        {
            Instance.ReceivePacketFromServer(packetData);
        }
    }

    public static void SendPacketToServer(AbstractPacket packet)
    {
        var packetData = PacketConverter.Serialize(packet);

        if (!PacketsNotLogged.Contains(packet.GetType()))
        {
            Log.Debug($"SendPacketToServer: {packetData}");
        }
        
        if (IsClient)
        {
            if (packet.IsReliable)
            {
                Instance.Rpc(nameof(ReceivePacketFromClient), packetData);
            }
            else
            {
                Instance.Rpc(nameof(ReceivePacketFromClientUnreliable), packetData);
            }
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

        if (!PacketsNotLogged.Contains(packet.GetType()))
        {
            Log.Debug($"SendPacketToPeer: {packetData}");
        }

        if (id == 0)
        {
            if (IsClient)
            {
                Instance.ReceivePacketFromServer(packetData);
                return;
            }
            throw new InvalidOperationException("Can't send packet to local client in dedicated server mode");
        }

        if (packet.IsReliable)
        {
            Instance.RpcId(id,nameof(ReceivePacketFromServer), packetData);
        }
        else
        {
            Instance.RpcId(id,nameof(ReceivePacketFromServerUnreliable), packetData);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = TransferModeEnum.Reliable, CallLocal = false)]
    internal void ReceivePacketFromClient(string packet)
    {
        if (!IsServer) return;
        var senderId = Api.GetRemoteSenderId();
        ReceivedRawPacket?.Invoke(senderId, packet);
        ReceivedPacket?.Invoke(ConfigurePacket(senderId, PacketConverter.Deserialize(packet)));
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = TransferModeEnum.Reliable, CallLocal = false)]
    internal void ReceivePacketFromServer(string packet)
    {
        if (!IsClient) return;
        ReceivedRawPacket?.Invoke(1, packet);
        ReceivedPacket?.Invoke(ConfigurePacket(1, PacketConverter.Deserialize(packet)));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = TransferModeEnum.Unreliable, CallLocal = false)]
    internal void ReceivePacketFromClientUnreliable(string packet)
    {
        if (!IsServer) return;
        var senderId = Api.GetRemoteSenderId();
        ReceivedRawPacket?.Invoke(senderId, packet);
        ReceivedPacket?.Invoke(ConfigurePacket(senderId, PacketConverter.Deserialize(packet)));
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = TransferModeEnum.Unreliable, CallLocal = false)]
    internal void ReceivePacketFromServerUnreliable(string packet)
    {
        if (!IsClient) return;
        ReceivedRawPacket?.Invoke(1, packet);
        ReceivedPacket?.Invoke(ConfigurePacket(1, PacketConverter.Deserialize(packet)));
    }

    private static AbstractPacket ConfigurePacket(int senderId, AbstractPacket packet)
    {
        packet.Sender = senderId;
        return packet;
    }
    private static void DefaultPacketProcessor(AbstractPacket packet)
    {
        
        if (packet.Sender.IsServer && packet is RegistrySynchronizationPacket syncPacket)
        {
            Log.Info("Received a sync packet");
            PacketRegistry.SynchronizeRegistry(syncPacket);
            return;
        }
    }
}