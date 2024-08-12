using System;
using System.IO;
using System.Linq;
using Godot;
using KludgeBox.Events.Global;

namespace KludgeBox.Networking;

public static partial class Network
{
    
    /// <summary>
    /// Broadcasts packet to all available peers.
    /// </summary>
    /// <param name="packet">Packet to send</param>
    public static void SendToAll(NetPacket packet)
    {
        Send(BroadcastId, packet, packet.Mode, packet.PreferredChannel);
    }
    
    public static void SendToAll(NetPacket packet, MultiplayerPeer.TransferModeEnum mode, int channel)
    {
        Send(BroadcastId, packet, packet.Mode, packet.PreferredChannel);
    }
    
    /// <summary>
    /// Sends packet from client to server.
    /// </summary>
    /// <param name="packet">Packet to send</param>
    public static void SendToServer(NetPacket packet)
    {
        Send(ServerId, packet, packet.Mode, packet.PreferredChannel);
    }
    
    /// <summary>
    /// Sends packet from client to server.
    /// </summary>
    /// <param name="packet">Packet to send</param>
    /// <param name="mode">Reliability mode (Reliable, Unreliable or UnreliableOrdered)</param>
    /// <param name="channel">Channel to send on.</param>
    public static void SendToServer(NetPacket packet, MultiplayerPeer.TransferModeEnum mode, int channel)
    {
        Send(ServerId, packet, packet.Mode, packet.PreferredChannel);
    }
    
    /// <summary>
    ///  specified peer.
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
    /// <param name="channel">Channel to send on. </param>
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
}