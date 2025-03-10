using Godot;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.KludgeBox.Networking.Packets;

namespace NeonWarfare.Scripts.Utils.Components;

public partial class NetworkInertiaComponent
{
    [GamePacket]
    public class SC_InertiaEntityPacket(long nid, long orderId, Vector2 position, float rotation,
        float movementRotation, float movementSpeed) : BinaryPacket
    {
        public override MultiplayerPeer.TransferModeEnum Mode => MultiplayerPeer.TransferModeEnum.Unreliable;
        
        public long Nid = nid;
        public long OrderId = orderId;
        public Vector2 Position = position;
        public float Rotation = rotation;
        public float MovementRotation = movementRotation;
        public float MovementSpeed = movementSpeed;

        public SC_InertiaEntityPacket(CS_InertiaEntityPacket packet) : 
            this(packet.Nid, packet.OrderId, packet.Position, packet.Rotation, packet.MovementRotation, packet.MovementSpeed) { }
    }
    
    [GamePacket]
    public class CS_InertiaEntityPacket(long nid, long orderId, Vector2 position, float rotation,
        float movementRotation, float movementSpeed) : BinaryPacket
    {
        public override MultiplayerPeer.TransferModeEnum Mode => MultiplayerPeer.TransferModeEnum.Unreliable;
        
        public long Nid = nid;
        public long OrderId = orderId;
        public Vector2 Position = position;
        public float Rotation = rotation;
        public float MovementRotation = movementRotation;
        public float MovementSpeed = movementSpeed;
    }
}
