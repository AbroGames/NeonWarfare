
using KludgeBox.Networking;

namespace NeonWarfare;

[GamePacket]
public class ServerSpawnEnemyPacket(long nid, double x, double y, double dir, bool isBoss) : BinaryPacket
{
    public long Nid = nid;
    public double X = x;
    public double Y = y;
    public double Dir = dir;
    public bool IsBoss = isBoss;
}

[GamePacket]
public class ServerSpawnEnemyBulletPacket(long nid, double x, double y, double dir, bool isBoss) : BinaryPacket
{
    public long Nid = nid;
    public double X = x;
    public double Y = y;
    public double Dir = dir;
    public bool IsBoss = isBoss;
}