using System;
using System.IO;
using System.Linq;
using Godot;
using KludgeBox.Events.Global;
using NeonWarfare;

namespace NeonWarfare.Scripts.KludgeBox.Networking;

public partial class Network
{
    
    public static void SendToAll(NetPacket packet)
    {
        SendToAll(packet, packet.Mode, packet.PreferredChannel);
    }
    
    public static void SendToAll(NetPacket packet, MultiplayerPeer.TransferModeEnum mode, int channel)
    {
        SendAsServer(BroadcastId, packet, mode, channel);
    }
    
    public static void SendToAllExclude(long excludeId, NetPacket packet)
    {
        SendToAllExclude(excludeId, packet, packet.Mode, packet.PreferredChannel);
    }
    
    public static void SendToAllExclude(long excludeId, NetPacket packet, MultiplayerPeer.TransferModeEnum mode, int channel)
    {
        int[] peers = ServerRoot.Instance.Game.Network.Api.GetPeers();
        foreach (var peerId in peers)
        {
            if (peerId == excludeId) continue;
            SendAsServer(peerId, packet, mode, channel);
        }
    }
    
    public static void SendToClient(long id, NetPacket packet)
    {
        SendToClient(id, packet, packet.Mode, packet.PreferredChannel);
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
