using System;
using System.Collections.Generic;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;

namespace NeonWarfare;

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
}