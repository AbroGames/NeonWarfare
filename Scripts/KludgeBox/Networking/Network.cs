using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Godot;
using KludgeBox.Events.Global;

namespace KludgeBox.Networking;


public partial class Network
{
    public const long BroadcastId = 0;
    public const long ServerId = 1;

    public bool IsServer => Mode is Netmode.Server;
    public bool IsClient => Mode is Netmode.Client;
    
    public Netmode Mode { get; set; }
    public PacketRegistry PacketRegistry { get; set; } = new PacketRegistry();
    public SceneMultiplayer Api { get; internal set; }

    public ENetMultiplayerPeer Peer
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
    internal void Initialize(SceneMultiplayer api)
    {
        // Avoid multiple event subscriptions.
        if (Api == api) return;
     
        Log.Debug("Init network");
        Api = api;
        Api.ServerDisconnected += CloseConnection;
        Api.PeerPacket += OnPacketReceived;
        
        Api.ConnectedToServer += () => EventBus.Publish(new ConnectedToServerEvent());
        Api.ConnectionFailed += () => EventBus.Publish(new ConnectionToServerFailedEvent());
        Api.PeerConnected += id => EventBus.Publish(new PeerConnectedEvent(id));
        Api.PeerDisconnected += id => EventBus.Publish(new PeerDisconnectedEvent(id));
        Api.ServerDisconnected += () => EventBus.Publish(new ServerDisconnectedEvent());
    }
    
    public Error SetServer(int port, int maxClients = 8)
    {
        Log.Debug("Create server network");
        Mode = Netmode.Server;
        PacketRegistry.ScanPackets();

        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateServer(port, maxClients);
        Peer = peer;
        
        return error;
    }

    public Error SetClient(string host, int port)
    {
        Log.Debug("Create client network");
        Mode = Netmode.Client;
        PacketRegistry.ScanPackets();
        
        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateClient(host, port);
        Peer = peer;
        
        return error;
    }

    public void CloseConnection()
    {
        Log.Debug("Close network connection");
        Peer?.Close();
    }
    
    public void SendRaw(long id, byte[] encodedPacketBuffer, MultiplayerPeer.TransferModeEnum mode = MultiplayerPeer.TransferModeEnum.Reliable, int channel = 0)
    {
        Api.SendBytes(encodedPacketBuffer, (int) id, mode, channel);
    }
    
    private void OnPacketReceived(long id, byte[] packet)
    {
        var packetObj = PacketHelper.DecodePacket(packet, PacketRegistry);
        packetObj.SenderId = id;
        //Log.Info($"Received packet from {id} with {packet.Length} - 4 bytes. Received type is {packetObj.GetType().FullName}");
        
        EventBus.Publish(packetObj);
    }
}