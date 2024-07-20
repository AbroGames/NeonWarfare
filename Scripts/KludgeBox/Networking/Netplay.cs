using System;
using System.IO;
using System.Linq;
using Godot;
using KludgeBox.Events.Global;

namespace KludgeBox.Networking;


public static partial class Netplay
{
    public const long BroadcastId = 0;
    public const long ServerId = 1;
    
    public static event Action ConnectedToServer;
    public static event Action ConnectionFailed;
    public static event Action<long> PeerConnected;
    public static event Action<long> PeerDisconnected;
    
    public static Netmode Mode { get; set; }
    public static PacketRegistry PacketRegistry { get; set; } = new PacketRegistry();
    public static SceneMultiplayer Api { get; internal set; }

    public static ENetMultiplayerPeer Peer
    {
        get => Api.MultiplayerPeer as ENetMultiplayerPeer;
        set => Api.MultiplayerPeer = value;
    }
    
    public enum Netmode
    {
        Client,
        Server
    }
    
    // Called from Root._Ready()
    // Seems like some sort of potential memory leak.
    internal static void Initialize(SceneMultiplayer api)
    {
        // Avoid multiple event subscriptions.
        if (Api == api) return;
        
        Api = api;
        Api.ConnectedToServer += () => ConnectedToServer?.Invoke();
        Api.ConnectionFailed += () => ConnectionFailed?.Invoke();
        Api.PeerConnected += id => PeerConnected?.Invoke(id);
        Api.PeerDisconnected += id => PeerDisconnected?.Invoke(id);
        Api.PeerPacket += OnPacketReceived;
    }
    
    public static Error SetServer(int port, int maxClients = 8)
    {
        Mode = Netmode.Server;

        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateServer(port, maxClients);
        Peer = peer;
        
        PacketRegistry.ScanPackets();
        return error;
    }

    public static Error SetClient(string host, int port)
    {
        Mode = Netmode.Client;
        
        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateClient(host, port);
        Peer = peer;
        
        PacketRegistry.ScanPackets();
        return error;
    }

    public static void CloseConnection()
    {
        Peer?.Close();
        PacketRegistry = new PacketRegistry();
    }
}