using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.KludgeBox.Networking.Packets;

namespace NeonWarfare.Scripts.Utils.Components;

public partial class NetworkInertiaComponent
{
    [GamePacket]
    public class SC_InertiaEntityPacket(long nid, float x, float y, float dir,
        float movementDir, float movementSpeed) : BinaryPacket
    {
        public long Nid = nid;
        public float X = x;
        public float Y = y;
        public float Dir = dir;
        public float MovementDir = movementDir;
        public float MovementSpeed = movementSpeed;

        public SC_InertiaEntityPacket(CS_InertiaEntityPacket packet) : 
            this(packet.Nid, packet.X, packet.Y, packet.Dir, packet.MovementDir, packet.MovementSpeed) { }
    }
    
    [GamePacket]
    public class CS_InertiaEntityPacket(long nid, float x, float y, float dir,
        float movementDir, float movementSpeed) : BinaryPacket
    {
        public long Nid = nid;
        public float X = x;
        public float Y = y;
        public float Dir = dir;
        public float MovementDir = movementDir;
        public float MovementSpeed = movementSpeed;
    }
}
