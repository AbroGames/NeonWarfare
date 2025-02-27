using Godot;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.KludgeBox.Networking.Packets;

namespace NeonWarfare.Scenes.World;

public abstract partial class ClientWorld
{
    
    [GamePacket]
    public class SC_PlayerSpawnPacket(long nid, Vector2 position, float dir, Color color) : BinaryPacket
    {
        public long Nid = nid;
        public Vector2 Position = position;
        public float Dir = dir;
        public Color Color = color;
    }
    
    [GamePacket]
    public class SC_AllySpawnPacket(long nid, Vector2 position, float dir, Color color, long id) : BinaryPacket
    {
        public long Nid = nid;
        public Vector2 Position = position;
        public float Dir = dir;
        public long Id = id;
        public Color Color = color;
    }
    
    [GamePacket]
    public class SC_EnemySpawnPacket(long nid, Vector2 position, float dir, Color color) : BinaryPacket
    {
        public long Nid = nid;
        public Vector2 Position = position;
        public float Dir = dir;
        public Color Color = color;
    }
    
    [GamePacket]
    public class SC_StaticEntitySpawnPacket(SC_StaticEntitySpawnPacket.StaticEntityType entityType, Vector2 position, Vector2 scale, float dir, Color color) : BinaryPacket
    {
        public enum StaticEntityType
        {
            Wall
        }

        public StaticEntityType EntityType = entityType;
        public Vector2 Position = position;
        public Vector2 Scale = scale;
        public float Dir = dir;
        public Color Color = color;

        public SC_StaticEntitySpawnPacket(StaticEntityType entityType, Vector2 position, float dir) :
            this(entityType, position, Vector2.One, dir, new Color(1, 1, 1)) {}
    }
}
