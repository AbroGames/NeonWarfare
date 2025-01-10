using KludgeBox.Networking;

namespace NeonWarfare;

[GamePacket]
public class ClientMovementPlayerPacket(long nid, float x, float y, float dir,
    float movementDir, float movementSpeed) : BinaryPacket
{
    public long Nid = nid;
    public float X = x;
    public float Y = y;
    public float Dir = dir;
    public float MovementDir = movementDir;
    public float MovementSpeed = movementSpeed;
}

[GamePacket]
public class ClientPlayerPrimaryAttackPacket(float x, float y, float dir) : BinaryPacket
{
    public float X = x;
    public float Y = y;
    public float Dir = dir;
}

[GamePacket]
public class ClientPlayerSecondaryAttackPacket(float x, float y, float dir) : BinaryPacket
{
    public float X = x;
    public float Y = y;
    public float Dir = dir;
}

[GamePacket]
public class ServerPlayerPrimaryAttackPacket(long nid, float x, float y, float dir, float movementSpeed) : BinaryPacket
{
    public long Nid = nid;
    public float X = x;
    public float Y = y;
    public float Dir = dir;
    public float MovementSpeed = movementSpeed;
}

[GamePacket]
public class ServerPlayerSecondaryAttackPacket(long nid, float x, float y, float dir, float movementSpeed) : BinaryPacket
{
    public long Nid = nid;
    public float X = x;
    public float Y = y;
    public float Dir = dir;
    public float MovementSpeed = movementSpeed;
}




