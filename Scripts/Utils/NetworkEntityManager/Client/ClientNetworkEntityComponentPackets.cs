using Godot;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.KludgeBox.Networking.Packets;

namespace NeonWarfare.Scripts.Utils.NetworkEntityManager.Client;

public partial class ClientNetworkEntityComponent
{

    [GamePacket]
    public class SC_PositionEntityPacket(long nid, float x, float y, float dir) : BinaryPacket
    {
        public override MultiplayerPeer.TransferModeEnum Mode => MultiplayerPeer.TransferModeEnum.Unreliable;
        
        public long Nid = nid;
        public float X = x;
        public float Y = y;
        public float Dir = dir;
    }

    [GamePacket]
    public class SC_DestroyEntityPacket(long nid) : BinaryPacket
    {
        public long Nid = nid;
    }
}
