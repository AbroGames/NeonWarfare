
using KludgeBox.Events;
using KludgeBox.Networking;

namespace NeonWarfare.NetOld.Server;


[GamePacket]
public class ServerSpawnPlayerPacket(long nid, double x, double y, double dir) : BinaryPacket
{
    public long Nid = nid;
    public double X = x;
    public double Y = y;
    public double Dir = dir;
}

[GamePacket]
public class ServerSpawnAllyPacket(long nid, double x, double y, double dir) : BinaryPacket
{
    public long Nid = nid;
    public double X = x;
    public double Y = y;
    public double Dir = dir;
}

[GamePacket]
public class ServerPositionEntityPacket(long nid, double x, double y, double dir) : BinaryPacket, IInstanceEvent
{
    public object NetworkId => Nid;
    
    public long Nid = nid;
    public double X = x;
    public double Y = y;
    public double Dir = dir;
}

[GamePacket]
public class ServerMovementEntityPacket(long nid, float x, float y, float dir,
    float movementDir, float movementSpeed) : BinaryPacket, IInstanceEvent
{
    public object NetworkId => Nid;
    
    public long Nid = nid;
    public float X = x;
    public float Y = y;
    public float Dir = dir;
    public float MovementDir = movementDir;
    public float MovementSpeed = movementSpeed;
}

[GamePacket]
public class ServerDestroyEntityPacket(long nid) : BinaryPacket
{
    public long Nid = nid;
}