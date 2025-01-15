using KludgeBox.Networking;

namespace NeonWarfare;

public partial class ClientPlayer  
{
    [GamePacket]
    public class SC_PlayerSpawnPacket(long nid, float x, float y, float dir) : BinaryPacket
    {
        public long Nid = nid;
        public float X = x;
        public float Y = y;
        public float Dir = dir;
    }
}