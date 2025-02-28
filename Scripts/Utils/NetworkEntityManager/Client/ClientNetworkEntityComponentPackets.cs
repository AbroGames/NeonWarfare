using Godot;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.KludgeBox.Networking.Packets;

namespace NeonWarfare.Scripts.Utils.NetworkEntityManager.Client;

public partial class ClientNetworkEntityComponent
{

    [GamePacket]
    public class SC_PositionEntityPacket(long nid, Vector2 position, float rotation) : BinaryPacket
    {
        public override MultiplayerPeer.TransferModeEnum Mode => MultiplayerPeer.TransferModeEnum.Unreliable;
        
        public long Nid = nid;
        public Vector2 Position = position;
        public float Rotation = rotation;
    }

    [GamePacket]
    public class SC_DestroyEntityPacket(long nid) : BinaryPacket
    {
        public long Nid = nid;
    }
}
