using Godot;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.KludgeBox.Networking.Packets;

namespace NeonWarfare.Scripts.Utils.Components;

public partial class NetworkInertiaComponent
{
    [GamePacket]
    public class SC_InertiaEntityPacket(long nid, Vector2 position, float dir,
        float movementDir, float movementSpeed) : BinaryPacket
    {
        public override MultiplayerPeer.TransferModeEnum Mode => MultiplayerPeer.TransferModeEnum.Unreliable;
        
        public long Nid = nid;
        public Vector2 Position = position;
        public float Dir = dir;
        public float MovementDir = movementDir;
        public float MovementSpeed = movementSpeed;

        public SC_InertiaEntityPacket(CS_InertiaEntityPacket packet) : 
            this(packet.Nid, packet.Position, packet.Dir, packet.MovementDir, packet.MovementSpeed) { }
    }
    
    [GamePacket]
    public class CS_InertiaEntityPacket(long nid, Vector2 position, float dir,
        float movementDir, float movementSpeed) : BinaryPacket
    {
        public override MultiplayerPeer.TransferModeEnum Mode => MultiplayerPeer.TransferModeEnum.Unreliable;
        
        public long Nid = nid;
        public Vector2 Position = position;
        public float Dir = dir;
        public float MovementDir = movementDir;
        public float MovementSpeed = movementSpeed;
    }
}
