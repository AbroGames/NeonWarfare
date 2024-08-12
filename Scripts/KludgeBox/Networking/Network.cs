using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Godot;
using KludgeBox.Events.Global;

namespace KludgeBox.Networking;


public static partial class Network
{
    public const long BroadcastId = 0;
    public const long ServerId = 1;

    public static bool IsServer => Mode is Netmode.Server;
    public static bool IsClient => Mode is Netmode.Client;
    
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
     
        Log.Debug("Init network connection");
        Api = api;
        Api.ServerDisconnected += CloseConnection;
        Api.PeerPacket += OnPacketReceived;
        
        Api.ConnectedToServer += () => EventBus.Publish(new ConnectedToServerEvent());
        Api.ConnectionFailed += () => EventBus.Publish(new ConnectionToServerFailedEvent());
        Api.PeerConnected += id => EventBus.Publish(new PeerConnectedEvent(id));
        Api.PeerDisconnected += id => EventBus.Publish(new PeerDisconnectedEvent(id));
        Api.ServerDisconnected += () => EventBus.Publish(new ServerDisconnectedEvent());
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
        Log.Debug("Close network connection");
        Peer?.Close();
        PacketRegistry = new PacketRegistry();
    }
}