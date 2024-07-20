using System;
using System.IO;
using System.Linq;
using Godot;
using KludgeBox.Events.Global;

namespace KludgeBox.Networking;

public enum Netmode
{
    Singleplayer,
    Client,
    Server
}
public static class Netplay
{
    public const long ServerId = 1;
    public const long BroadcastId = 0;
    
    public static event Action ConnectedToServer;
    public static event Action ConnectionFailed;
    public static event Action<long> PeerConnected;
    public static event Action<long> PeerDisconnected;
    
    static Netplay()
    {
        
    }
    
    public static Netmode Mode { get; set; } = Netmode.Singleplayer;

    public static bool IsServer => Mode is Netmode.Server;
    public static bool IsClient => Mode is Netmode.Client;
    public static bool IsSingleplayer => Mode is Netmode.Singleplayer;
    public static PacketRegistry PacketRegistry { get; set; } = new PacketRegistry();
    
    /// <summary>
    /// Socket to send and receive packets from server. Active only in Client mode.
    /// </summary>
    public static ISocket ServerSocket { get; internal set; }

    public static ENetMultiplayerPeer Peer
    {
        get => Api.MultiplayerPeer as ENetMultiplayerPeer;
        set => Api.MultiplayerPeer = value;
    }
    public static SceneMultiplayer Api { get; internal set; }
    public static void SetServer(int port, int maxClients = 5)
    {
        Mode = Netmode.Server;
        ResetConnection();

        var peer = new ENetMultiplayerPeer();
        peer.CreateServer(port, maxClients);
        
        Peer = peer;
    }

    public static void SetClient(string host, int port)
    {
        Mode = Netmode.Client;
        ResetConnection();
        var peer = new ENetMultiplayerPeer();
        peer.CreateClient(host, port);
        
        Peer = peer;
    }

    public static void SetSingleplayer()
    {
        Mode = Netmode.Singleplayer;
        ResetConnection();
    }

    public static void ResetConnection()
    {
        Peer?.Close();
    }

    /// <summary>
    /// Sends packet to all available peers.
    /// </summary>
    public static void Send(NetPacket packet)
    {
        Send(BroadcastId, packet);
    }
    
    /// <summary>
    /// Sends packet to specified peer.
    /// </summary>
    /// <param name="id">ID of the peer. 0 for broadcast and 1 for server.</param>
    public static void Send(long id, NetPacket packet)
    {
        var bytes = PacketHelper.EncodePacket(packet);
        //Log.Info($"Sending packet to {id} with {packetData.Length} + 4 bytes. Expected type is {packet.GetType().FullName}");
        Api.SendBytes(bytes, (int)id, MultiplayerPeer.TransferModeEnum.Reliable, packet.PreferredChannel);
    }

    // Called from Root._Ready()
    internal static void Initialize(SceneMultiplayer api)
    {
        Api = api;
        Api.ConnectedToServer += () => ConnectedToServer?.Invoke();
        Api.ConnectionFailed += () => ConnectionFailed?.Invoke();
        Api.PeerConnected += id => PeerConnected?.Invoke(id);
        Api.PeerDisconnected += id => PeerDisconnected?.Invoke(id);
        Api.PeerPacket += OnPacketReceived;
        
        ResetConnection();
    }
    
    

    internal static void OnPacketReceived(long id, byte[] packet)
    {
        var packetObj = PacketHelper.DecodePacket(packet);
        packetObj.SenderId = id;
        //Log.Info($"Received packet from {id} with {packet.Length} - 4 bytes. Received type is {packetObj.GetType().FullName}");
        
        EventBus.Publish(packetObj);
    }
}