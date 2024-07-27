
using KludgeBox.Events;
using KludgeBox.Networking;

namespace NeonWarfare.NetOld.Server;

[GamePacket]
public class WaitBattleEndPacket : BinaryPacket;

[GamePacket]
public class ChangeWorldPacket(ChangeWorldPacket.ServerWorldType worldType) : BinaryPacket
{
    public enum ServerWorldType { Safe, Battle };
    public ServerWorldType WorldType = worldType;
}

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
public class ServerMovementEntityPacket(long nid, double x, double y, double dir,
    double movementX, double movementY, double movementSpeed) : BinaryPacket
{
    public long Nid = nid;
    public double X = x;
    public double Y = y;
    public double Dir = dir;
    public double MovementX = movementX;
    public double MovementY = movementY;
    public double MovementSpeed = movementSpeed;
}

[GamePacket]
public class ServerDestroyEntityPacket(long nid) : BinaryPacket
{
    public long Nid = nid;
}