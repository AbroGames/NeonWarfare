using KludgeBox.Events;
using KludgeBox.Net;
using KludgeBox.Net.Packets;

namespace NeoVector;

[GamePacket]
public class ClientMovementPlayerPacket(long nid, double x, double y, double dir,
    double movementX, double movementY, double movementSpeed) : AbstractPacket
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
public class ClientPlayerPrimaryAttackPacket(double x, double y, double dir) : AbstractPacket
{
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
    public double Dir { get; set; } = dir;
}

[GamePacket]
public class ClientPlayerSecondaryAttackPacket(double x, double y, double dir) : AbstractPacket
{
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
    public double Dir { get; set; } = dir;
}

[GamePacket]
public class ServerPlayerPrimaryAttackPacket(long nid, double x, double y, double dir, double movementSpeed) : AbstractPacket
{
    public long Nid { get; set; } = nid;
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
    public double Dir { get; set; } = dir;
    public double MovementSpeed { get; set; } = movementSpeed;
}

[GamePacket]
public class ServerPlayerSecondaryAttackPacket(long nid, double x, double y, double dir, double movementSpeed) : AbstractPacket
{
    public long Nid { get; set; } = nid;
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
    public double Dir { get; set; } = dir;
    public double MovementSpeed { get; set; } = movementSpeed;
}




