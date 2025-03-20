using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.Root.ServerRoot;
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
    public class SC_EnemySpawnPacket(SC_EnemySpawnPacket.EnemyType type, long nid, Vector2 position, float rotation, Color color) : BinaryPacket
    {
        public EnemyType Type = type;
        public long Nid = nid;
        public Vector2 Position = position;
        public float Rotation = rotation;
        public Color Color = color;
        public PackedScene EnemyClientScene => EnemyScenesMap[Type].ClientScene.Invoke();
        public PackedScene EnemyServerScene => EnemyScenesMap[Type].ServerScene.Invoke();
        
        public enum EnemyType
        {
            Zerg, Shooter, Turtle
        }

        public record EnemyScenes(Func<PackedScene> ClientScene, Func<PackedScene> ServerScene);
        public static readonly IReadOnlyDictionary<EnemyType, EnemyScenes> EnemyScenesMap = new System.Collections.Generic.Dictionary<EnemyType, EnemyScenes>
        {
            { EnemyType.Zerg, new(() => ClientRoot.Instance.PackedScenes.ZergEnemy, () => ServerRoot.Instance.PackedScenes.ZergEnemy) },
            { EnemyType.Shooter, new(() => ClientRoot.Instance.PackedScenes.ShooterEnemy, () => ServerRoot.Instance.PackedScenes.ShooterEnemy) },
            { EnemyType.Turtle, new(() => ClientRoot.Instance.PackedScenes.TurtleEnemy, () => ServerRoot.Instance.PackedScenes.TurtleEnemy) } 
        };
        
        public SC_EnemySpawnPacket(EnemyType type, long nid, Vector2 position, float rotation) : this(type, nid, position, rotation, new Color(0, 0, 0, 0)) {}
    }
    
    [GamePacket]
    public class SC_StaticEntitySpawnPacket(SC_StaticEntitySpawnPacket.StaticEntityType type, Vector2 position, Vector2 scale, float rotation, Color color) : BinaryPacket
    {
        public StaticEntityType Type = type;
        public Vector2 Position = position;
        public Vector2 Scale = scale;
        public float Rotation = rotation;
        public Color Color = color;
        public PackedScene StaticEntityClientScene => StaticEntityScenesMap[Type].ClientScene.Invoke();
        public PackedScene StaticEntityServerScene => StaticEntityScenesMap[Type].ServerScene.Invoke();
        
        public enum StaticEntityType
        {
            Wall, Border
        }

        public record StaticEntityScenes(Func<PackedScene> ClientScene, Func<PackedScene> ServerScene);
        public static readonly IReadOnlyDictionary<StaticEntityType, StaticEntityScenes> StaticEntityScenesMap = new System.Collections.Generic.Dictionary<StaticEntityType, StaticEntityScenes>
        {
            { StaticEntityType.Wall, new(() => ClientRoot.Instance.PackedScenes.Wall, () => ServerRoot.Instance.PackedScenes.Wall) },
            { StaticEntityType.Border, new(() => ClientRoot.Instance.PackedScenes.Wall, () => ServerRoot.Instance.PackedScenes.Wall) } 
        };

        public SC_StaticEntitySpawnPacket(StaticEntityType type, Vector2 position, float rotation) :
            this(type, position, Vector2.One, rotation, new Color(1, 1, 1)) {}
    }
}
