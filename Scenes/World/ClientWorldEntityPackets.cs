using Godot;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.KludgeBox.Networking.Packets;

namespace NeonWarfare.Scenes.World;

public abstract partial class ClientWorld
{
    
    [GamePacket]
    public class SC_PlayerSpawnPacket(long nid, float x, float y, float dir) : BinaryPacket
    {
        public long Nid = nid;
        public float X = x;
        public float Y = y;
        public float Dir = dir;
    }
    
    [GamePacket]
    public class SC_AllySpawnPacket(long nid, float x, float y, float dir, long id) : BinaryPacket
    {
        public long Nid = nid;
        public float X = x;
        public float Y = y;
        public float Dir = dir;
        public long Id = id;
    }
    
    [GamePacket]
    public class SC_EnemySpawnPacket(long nid, float x, float y, float dir) : BinaryPacket
    {
        public long Nid = nid;
        public float X = x;
        public float Y = y;
        public float Dir = dir;
    }
    
    [GamePacket]
    public class SC_BulletSpawnPacket(long nid, Vector2 startPosition, float rotation, Color color) : BinaryPacket //TODO del after test
    {
        public long Nid = nid;
        public Vector2 StartPosition = startPosition;
        public float Rotation = rotation;
        public Color Color = color;
    }
}
