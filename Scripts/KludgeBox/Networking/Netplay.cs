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
    public const long BroadcastId = 0;
    
    public static event Action ConnectedToServer;
    public static event Action ConnectionFailed;
    public static event Action<long> PeerConnected;
    public static event Action<long> PeerDisconnected;
    
    public static Netmode Mode { get; set; }
    public static PacketRegistry PacketRegistry { get; set; }
    public static SceneMultiplayer Api { get; internal set; }

    public static ENetMultiplayerPeer Peer
    {
        get => Api.MultiplayerPeer as ENetMultiplayerPeer;
        set => Api.MultiplayerPeer = value;
    }
    
    public static void SetServer(int port, int maxClients = 8)
    {
        Mode = Netmode.Server;

        var peer = new ENetMultiplayerPeer();
        peer.CreateServer(port, maxClients);
        Peer = peer;
        
        PacketRegistry.ScanPackets();
    }

    public static void SetClient(string host, int port)
    {
        Mode = Netmode.Client;
        
        var peer = new ENetMultiplayerPeer();
        peer.CreateClient(host, port);
        Peer = peer;
        
        PacketRegistry.ScanPackets();
    }

    public static void CloseConnection()
    {
        Peer?.Close();
        PacketRegistry = new PacketRegistry();
    }
    
    /// <summary>
    /// Broadcasts packet to all available peers.
    /// </summary>
    /// <param name="packet">Packet to send</param>
    public static void Send(NetPacket packet)
    {
        Send(BroadcastId, packet, packet.Mode, packet.PreferredChannel);
    }
    
    /// <summary>
    /// Broadcasts packet to all available peers.
    /// </summary>
    /// <param name="packet">Packet to send</param>
    /// <param name="mode">Reliability mode (Reliable, Unreliable or UnreliableOrdered)</param>
    /// <param name="channel">Channel to send on. -1 means use packet's preferred channel</param>
    public static void Send(NetPacket packet, MultiplayerPeer.TransferModeEnum mode, int channel)
    {
        Send(BroadcastId, packet, packet.Mode, packet.PreferredChannel);
    }
    
    /// <summary>
    /// Sends packet to specified peer.
    /// </summary>
    /// <param name="id">Peer id to send to. 0 for broadcast, 1 for server.</param>
    /// <param name="packet">Packet to send</param>
    public static void Send(long id, NetPacket packet)
    {
        Send(id, packet, packet.Mode, packet.PreferredChannel);
    }
    
    /// <summary>
    /// Sends packet to specified peer.
    /// </summary>
    /// <param name="id">Peer id to send to. 0 for broadcast, 1 for server.</param>
    /// <param name="packet">Packet to send</param>
    /// <param name="mode">Reliability mode (Reliable, Unreliable or UnreliableOrdered)</param>
    /// <param name="channel">Channel to send on. -1 means use packet's preferred channel</param>
    public static void Send(long id, NetPacket packet, MultiplayerPeer.TransferModeEnum mode, int channel)
    {
        var bytes = PacketHelper.EncodePacket(packet);
        SendRaw(id, bytes, mode, channel);
    }

    /// <summary>
    /// Sends raw bytes to specified peer. Receiving peer will try to decode it anyways.
    /// </summary>
    /// <param name="id">Peer id to send to. 0 for broadcast, 1 for server.</param>
    /// <param name="encodedPacketBuffer">Expected to be an encoded packet (like PacketHelper.EncodePacket(packet.ToBuffer()))</param>
    /// <param name="mode">Reliability mode (Reliable, Unreliable or UnreliableOrdered)</param>
    /// <param name="channel">Channel to send on. -1 means use packet's preferred channel</param>
    /// <remarks>
    /// This can be used to avoid multiple calls to <see cref="PacketHelper.EncodePacket"/> and <see cref="NetPacket.ToBuffer"/> for sending one packet to multiple specified peers.
    /// </remarks>
    public static void SendRaw(long id, byte[] encodedPacketBuffer, MultiplayerPeer.TransferModeEnum mode = MultiplayerPeer.TransferModeEnum.Reliable, int channel = 0)
    {
        Api.SendBytes(encodedPacketBuffer, (int) id, mode, channel);
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
    
    internal static void OnPacketReceived(long id, byte[] packet)
    {
        var packetObj = PacketHelper.DecodePacket(packet);
        packetObj.SenderId = id;
        //Log.Info($"Received packet from {id} with {packet.Length} - 4 bytes. Received type is {packetObj.GetType().FullName}");
        
        EventBus.Publish(packetObj);
    }
}