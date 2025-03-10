using System.Collections.Generic;
using System.Linq;
using Godot;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scripts.Utils.MapGenerator;

public class MapGenerator
{

    public readonly long Seed;
    
    public MapGenerator() : this((long) Rand.Int * Rand.Int) { }
    
    public MapGenerator(long seed)
    {
        Seed = seed;
    }

    /*
     TODO Временно возвращаем SC_StaticEntitySpawnPacket.
     В целевой версии должны возвращать DTO-прослойку, которая потом будет использована для:
        1) Создание SC_StaticEntitySpawnPacket и отправка на клиент
        2) Создание ноды-сцены и спаун её на сервере
     */
    public List<Location> Generate()
    {
        Color gray = new(0.3f, 0.3f, 0.3f);
        Color yellow = new(0.6f, 0.6f, 0.0f);
        Color green = new(0.0f, 0.6f, 0.0f);
        Color blue = new(0.0f, 0.0f, 0.6f);
        Color red = new(0.6f, 0.0f, 0.0f);
        Location mainLocation = new Location(new Vector2(0, 0), [
            new(Entity.EntityType.Wall, new Vector2(-5187, 56), new Vector2(1, 20), 0, yellow),
            new(Entity.EntityType.Wall, new Vector2(4516, 12), new Vector2(1, 20), 0, green),
            new(Entity.EntityType.Wall, new Vector2(-332, 5341), new Vector2(20, 1), 0, blue),
            new(Entity.EntityType.Wall, new Vector2(-332, -5327), new Vector2(20, 1), 0, red),
            new(Entity.EntityType.Wall, new Vector2(-2492, -3283), new Vector2(0.2f, 2), 0.943161f, gray),
            new(Entity.EntityType.Wall, new Vector2(1782, -1297), new Vector2(0.2f, 2), 2.09439f, gray),
            new(Entity.EntityType.Wall, new Vector2(-1878, -1367), new Vector2(0.2f, 2), 0.261799f, gray),
            new(Entity.EntityType.Wall, new Vector2(3235, -1343), new Vector2(0.2f, 3), 2.61799f, gray),
            new(Entity.EntityType.Wall, new Vector2(1014, -2456), new Vector2(0.2f, 3), 1.0472f, gray),
            new(Entity.EntityType.Wall, new Vector2(-3874, -707), new Vector2(0.2f, 2), 3.66519f, gray),
            new(Entity.EntityType.Wall, new Vector2(-3433, -1995), new Vector2(0.2f, 3), 2.09439f, gray),
            new(Entity.EntityType.Wall, new Vector2(-449, -2860), new Vector2(0.2f, 3), 0.943161f, gray),
            new(Entity.EntityType.Wall, new Vector2(3034, -2664), new Vector2(0.2f, 3), 1.83259f, gray),
            new(Entity.EntityType.Wall, new Vector2(1347, -4071), new Vector2(0.2f, 3), 0.943161f, gray),
            new(Entity.EntityType.Wall, new Vector2(3306, -3894), new Vector2(0.2f, 2), -0.785397f, gray),
            new(Entity.EntityType.Wall, new Vector2(-1331, -4165), new Vector2(0.2f, 2), 2.87979f, gray),
            new(Entity.EntityType.Wall, new Vector2(-3713, -3809), new Vector2(0.2f, 2), -1.0472f, gray),
            new(Entity.EntityType.Wall, new Vector2(-2248, 1072), new Vector2(0.2f, 2), 0.523598f, gray),
            new(Entity.EntityType.Wall, new Vector2(2058, 4098), new Vector2(0.2f, 2), 2.09439f, gray),
            new(Entity.EntityType.Wall, new Vector2(-1858, 3532), new Vector2(0.2f, 2), 0.261799f, gray),
            new(Entity.EntityType.Wall, new Vector2(3255, 3556), new Vector2(0.2f, 3), 2.61799f, gray),
            new(Entity.EntityType.Wall, new Vector2(1290, 2683), new Vector2(0.2f, 3), 1.0472f, gray),
            new(Entity.EntityType.Wall, new Vector2(-3854, 4192), new Vector2(0.2f, 2), 3.66519f, gray),
            new(Entity.EntityType.Wall, new Vector2(-3349, 2712), new Vector2(0.2f, 3), 2.09439f, gray),
            new(Entity.EntityType.Wall, new Vector2(-288, 4176), new Vector2(0.2f, 3), 0.943161f, gray),
            new(Entity.EntityType.Wall, new Vector2(3054, 2235), new Vector2(0.2f, 3), 1.83259f, gray),
            new(Entity.EntityType.Wall, new Vector2(1799, 1052), new Vector2(0.2f, 3), 0.943161f, gray),
            new(Entity.EntityType.Wall, new Vector2(3326, 1005), new Vector2(0.2f, 2), -0.785397f, gray),
            new(Entity.EntityType.Wall, new Vector2(-1012, 2093), new Vector2(0.2f, 2), 2.35619f, gray),
            new(Entity.EntityType.Wall, new Vector2(-3693, 1090), new Vector2(0.2f, 2), -1.0472f, gray)
        ]);
        return [mainLocation];
    }
    
    public class Location(Vector2 position, List<Entity> entiries)
    {
        public Vector2 Position = position;
        public List<Entity> Entities = entiries;
        public List<Vector2> GetBorderCoordinates()
        {
            return Entities
                .FindAll(entity => entity.Type == Entity.EntityType.Border)
                .Select(entity => new Vector2(
                    entity.Position.X + entity.Position.X < Position.X ? (256 * entity.Scale.X) : (-1 * 256 * entity.Scale.X), 
                    entity.Position.Y + entity.Position.Y < Position.Y ? (256 * entity.Scale.Y) : (-1 * 256 * entity.Scale.Y)))
                .ToList();
        }
        public Location(Vector2 locationPosition) :
            this(locationPosition, new List<Entity>()) {}
    }
    
    public class Entity(Entity.EntityType type, Vector2 position, Vector2 scale, float rotation, Color color)
    {
        //TODO дублируется с StaticEntityType
        public enum EntityType
        {
            Wall, Border
        }

        public EntityType Type = type;
        public Vector2 Position = position;
        public Vector2 Scale = scale;
        public float Rotation = rotation;
        public Color Color = color;
        
    }
}