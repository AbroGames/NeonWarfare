using KludgeBox.Networking;

namespace NeonWarfare;

[GamePacket]
public class ClientMovementPlayerPacket(long nid, double x, double y, double dir,
    double movementDir, double movementSpeed) : BinaryPacket
{
    public long Nid = nid;
    public double X = x;
    public double Y = y;
    public double Dir = dir;
    public double MovementDir = movementDir;
    public double MovementSpeed = movementSpeed;
}

[GamePacket]
public class ClientPlayerPrimaryAttackPacket(double x, double y, double dir) : BinaryPacket
{
    public double X = x;
    public double Y = y;
    public double Dir = dir;
}

[GamePacket]
public class ClientPlayerSecondaryAttackPacket(double x, double y, double dir) : BinaryPacket
{
    public double X = x;
    public double Y = y;
    public double Dir = dir;
}

[GamePacket]
public class ServerPlayerPrimaryAttackPacket(long nid, double x, double y, double dir, double movementSpeed) : BinaryPacket
{
    public long Nid = nid;
    public double X = x;
    public double Y = y;
    public double Dir = dir;
    public double MovementSpeed = movementSpeed;
}

[GamePacket]
public class ServerPlayerSecondaryAttackPacket(long nid, double x, double y, double dir, double movementSpeed) : BinaryPacket
{
    public long Nid = nid;
    public double X = x;
    public double Y = y;
    public double Dir = dir;
    public double MovementSpeed = movementSpeed;
}




