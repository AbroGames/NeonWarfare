using KludgeBox.Events;
using KludgeBox.Net;
using KludgeBox.Net.Packets;

namespace NeoVector;

[GamePacket]
public class ServerWaitBattleEndPacket : AbstractPacket;

[GamePacket]
public class ServerChangeWorldPacket(ServerChangeWorldPacket.ServerWorldType worldType) : AbstractPacket
{
    
    public enum ServerWorldType { Unknown, Safe, Battle };
    public ServerWorldType WorldType { get; set; } = worldType;
}

[GamePacket]
public class ServerSpawnPlayerPacket(long nid, double x, double y, double dir) : AbstractPacket
{
    public long Nid { get; set; } = nid;
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
    public double Dir { get; set; } = dir;
}

[GamePacket]
public class ServerSpawnAllyPacket(long nid, double x, double y, double dir) : AbstractPacket
{
    public long Nid { get; set; } = nid;
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
    public double Dir { get; set; } = dir;
}

[GamePacket]
public class ServerPositionEntityPacket(long nid, double x, double y, double dir) : AbstractPacket
{
    public long Nid { get; set; } = nid;
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
    public double Dir { get; set; } = dir;
}