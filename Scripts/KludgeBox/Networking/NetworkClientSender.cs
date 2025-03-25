using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scripts.KludgeBox.Networking.Packets;
using NeonWarfare.Scripts.Utils.Profiling;

namespace NeonWarfare.Scripts.KludgeBox.Networking;

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
        var networkEvent = new OutgoingNetworkProfilingEvent(
                packet: packet,
                size: bytes.Length
            );
        ProfilingContainer.AddEvent(networkEvent);
        
        ClientRoot.Instance.Game.Network.SendRaw(id, bytes, mode, channel);
    }
}
