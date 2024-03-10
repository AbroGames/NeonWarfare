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




