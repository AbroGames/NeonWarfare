using System;
using System.IO;
using System.Linq;
using Godot;
using KludgeBox.Events.Global;
using NeonWarfare;

namespace KludgeBox.Networking;

public partial class Network
{
    
    
    public static void SendToServer(NetPacket packet)
    {
        SendAsClient(ServerId, packet, packet.Mode, packet.PreferredChannel);
    }
    
    public static void SendToServer(NetPacket packet, MultiplayerPeer.TransferModeEnum mode, int channel)
    {
        SendAsClient(ServerId, packet, mode, channel);
    }
    
    private static void SendAsClient(long id, NetPacket packet, MultiplayerPeer.TransferModeEnum mode, int channel)
    {
        var bytes = PacketHelper.EncodePacket(packet, ClientRoot.Instance.Game.Network.PacketRegistry);
        ClientRoot.Instance.Game.Network.SendRaw(id, bytes, mode, channel);
    }
}