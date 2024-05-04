using KludgeBox.Net;
using KludgeBox.Net.Packets;

namespace NeonWarfare;

[GamePacket]
public class ServerSpawnEnemyPacket(long nid, double x, double y, double dir, bool isBoss) : AbstractPacket
{
    public long Nid { get; set; } = nid;
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
    public double Dir { get; set; } = dir;
    public bool IsBoss { get; set; } = isBoss;
}

[GamePacket]
public class ServerSpawnEnemyBulletPacket(long nid, double x, double y, double dir, bool isBoss) : AbstractPacket
{
    public long Nid { get; set; } = nid;
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
    public double Dir { get; set; } = dir;
    public bool IsBoss { get; set; } = isBoss;
}