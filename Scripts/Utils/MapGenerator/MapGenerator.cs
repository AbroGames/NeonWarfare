using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Core;

using static NeonWarfare.Scenes.World.ClientWorld.SC_StaticEntitySpawnPacket.StaticEntityType;

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
    public List<ClientWorld.SC_StaticEntitySpawnPacket> Generate()
    {
        Color gray = new(0.2f, 0.2f, 0.2f);
        return new List<ClientWorld.SC_StaticEntitySpawnPacket>
        {
            new(Wall, new Vector2(400, 400), new Vector2(0.5f, 2), 0, gray),
            new(Wall, new Vector2(-400, -400), new Vector2(2, 0.5f), 0, gray)
        };
    }
}