using KludgeBox.Events;
using KludgeBox.Networking;

public partial class NetworkEntityComponent
{

    [GamePacket]
    public class SC_PositionEntityPacket(long nid, float x, float y, float dir) : BinaryPacket
    {
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