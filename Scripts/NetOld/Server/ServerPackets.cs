
using KludgeBox.Networking;

namespace NeonWarfare.NetOld.Server;

[GamePacket]
public class ServerWaitBattleEndPacket : BinaryPacket;

[GamePacket]
public class ServerChangeWorldPacket(ServerChangeWorldPacket.ServerWorldType worldType) : NetPacket
{
    
    public enum ServerWorldType { Unknown, Safe, Battle };
    public ServerWorldType WorldType { get; set; } = worldType;
}

[GamePacket]
public class ServerSpawnPlayerPacket(long nid, double x, double y, double dir) : BinaryPacket
{
    public long Nid { get; set; } = nid;
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
    public double Dir { get; set; } = dir;
}

[GamePacket]
public class ServerSpawnAllyPacket(long nid, double x, double y, double dir) : BinaryPacket
{
    public long Nid { get; set; } = nid;
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
    public double Dir { get; set; } = dir;
}

[GamePacket]
public class ServerPositionEntityPacket(long nid, double x, double y, double dir) : BinaryPacket
{
    public long Nid { get; set; } = nid;
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
    public double Dir { get; set; } = dir;
}

[GamePacket]
public class ServerMovementEntityPacket(long nid, double x, double y, double dir,
    double movementX, double movementY, double movementSpeed) : BinaryPacket
{
    public long Nid { get; set; } = nid;
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
    public double Dir { get; set; } = dir;
    public double MovementX { get; set; } = movementX;
    public double MovementY { get; set; } = movementY;
    public double MovementSpeed { get; set; } = movementSpeed;
}

[GamePacket]
public class ServerDestroyEntityPacket(long nid) : BinaryPacket
{
    public long Nid { get; set; } = nid;
}