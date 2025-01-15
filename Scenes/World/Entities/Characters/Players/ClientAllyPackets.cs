using KludgeBox.Networking;

namespace NeonWarfare;

public partial class ClientAlly  
{
    [GamePacket]
    public class SC_AllySpawnPacket(long nid, float x, float y, float dir, long id) : BinaryPacket
    {
        public long Nid = nid;
        public float X = x;
        public float Y = y;
        public float Dir = dir;
        public long Id = id;
    }
}