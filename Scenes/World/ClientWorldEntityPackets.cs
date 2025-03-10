using System.Collections.Generic;
using Godot;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.KludgeBox.Networking.Packets;

namespace NeonWarfare.Scenes.World;

public abstract partial class ClientWorld
{
    
    [GamePacket]
    public class SC_PlayerSpawnPacket(long nid, Vector2 position, float rotation, Color color) : BinaryPacket
    {
        public long Nid = nid;
        public Vector2 Position = position;
        public float Rotation = rotation;
        public Color Color = color;
    }
    
    [GamePacket]
    public class SC_AllySpawnPacket(long nid, Vector2 position, float rotation, Color color, long id) : BinaryPacket
    {
        public long Nid = nid;
        public Vector2 Position = position;
        public float Rotation = rotation;
        public long Id = id;
        public Color Color = color;
    }
    
    [GamePacket]
    public class SC_EnemySpawnPacket(long nid, Vector2 position, float rotation, Color color) : BinaryPacket
    {
        public long Nid = nid;
        public Vector2 Position = position;
        public float Rotation = rotation;
        public Color Color = color;
    }
    
    public class SC_LocationSpawnPacket(Vector2 locationPosition, List<SC_StaticEntitySpawnPacket> entiries) : BinaryPacket
    {
        public Vector2 LocationPosition = locationPosition;
        public List<SC_StaticEntitySpawnPacket> Entities = entiries;
        public List<Vector2> GetBorderCoordinates()
        {
            List<Vector2> borderPoints = [];
            foreach (var entity in Entities)
            {
                if (entity.EntityType == SC_StaticEntitySpawnPacket.StaticEntityType.Border)
                {
                    //TODO лучше получать динамически из объектов стены
                    borderPoints.Add(new Vector2(
                        entity.Position.X + entity.Position.X < LocationPosition.X ? (256 * entity.Scale.X) : (-1 * 256 * entity.Scale.X), 
                        entity.Position.Y + entity.Position.Y < LocationPosition.Y ? (256 * entity.Scale.Y) : (-1 * 256 * entity.Scale.Y)));
                }
            }

            return borderPoints;
        }
        public SC_LocationSpawnPacket(Vector2 locationPosition) :
            this(locationPosition, new List<SC_StaticEntitySpawnPacket>()) {}
    }
    
    [GamePacket]
    public class SC_StaticEntitySpawnPacket(SC_StaticEntitySpawnPacket.StaticEntityType entityType, Vector2 position, Vector2 scale, float rotation, Color color) : BinaryPacket
    {
        public enum StaticEntityType
        {
            Wall, Border
        }

        public StaticEntityType EntityType = entityType;
        public Vector2 Position = position;
        public Vector2 Scale = scale;
        public float Rotation = rotation;
        public Color Color = color;

        public SC_StaticEntitySpawnPacket(StaticEntityType entityType, Vector2 position, float rotation) :
            this(entityType, position, Vector2.One, rotation, new Color(1, 1, 1)) {}
    }
}
