using Godot;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.KludgeBox.Networking.Packets;

namespace NeonWarfare.Scenes.World;

public abstract partial class ClientWorld
{
    
    [GamePacket]
    public class SC_PlayerSpawnPacket(long nid, float x, float y, float dir, Color color) : BinaryPacket
    {
        public long Nid = nid;
        public float X = x;
        public float Y = y;
        public float Dir = dir;
        public Color Color = color;
    }
    
    [GamePacket]
    public class SC_AllySpawnPacket(long nid, float x, float y, float dir, Color color, long id) : BinaryPacket
    {
        public long Nid = nid;
        public float X = x;
        public float Y = y;
        public float Dir = dir;
        public long Id = id;
        public Color Color = color;
    }
    
    [GamePacket]
    public class SC_EnemySpawnPacket(long nid, float x, float y, float dir, Color color) : BinaryPacket
    {
        public long Nid = nid;
        public float X = x;
        public float Y = y;
        public float Dir = dir;
        public Color Color = color;
    }
}
