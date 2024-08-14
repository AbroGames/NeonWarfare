using System;
using System.IO;
using System.Linq;
using Godot;
using KludgeBox.Events.Global;
using NeonWarfare;

namespace KludgeBox.Networking;

public partial class Network
{
    
    public static void SendToAll(NetPacket packet)
    {
        SendAsServer(BroadcastId, packet, packet.Mode, packet.PreferredChannel);
    }
    
    public static void SendToAll(NetPacket packet, MultiplayerPeer.TransferModeEnum mode, int channel)
    {
        SendAsServer(BroadcastId, packet, mode, channel);
    }
    
    public static void SendToClient(long id, NetPacket packet)
    {
        SendAsServer(id, packet, packet.Mode, packet.PreferredChannel);
    }
    
    public static void SendToClient(long id, NetPacket packet, MultiplayerPeer.TransferModeEnum mode, int channel)
    {
        SendAsServer(id, packet, mode, channel);
    }
    
    private static void SendAsServer(long id, NetPacket packet, MultiplayerPeer.TransferModeEnum mode, int channel)
    {
        var bytes = PacketHelper.EncodePacket(packet, ServerRoot.Instance.Game.Network.PacketRegistry);
        ServerRoot.Instance.Game.Network.SendRaw(id, bytes, mode, channel);
    }
}