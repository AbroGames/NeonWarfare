using System.Collections.Generic;
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
    public List<ClientWorld.SC_StaticEntitySpawnPacket> Generate()
    {
        return new List<ClientWorld.SC_StaticEntitySpawnPacket>
        {
            new ClientWorld.SC_StaticEntitySpawnPacket(ClientWorld.SC_StaticEntitySpawnPacket.StaticEntityType.Wall, new Vector2(200, 200), 0),
            new ClientWorld.SC_StaticEntitySpawnPacket(ClientWorld.SC_StaticEntitySpawnPacket.StaticEntityType.Wall, new Vector2(-200, -200), 0)
        };
    }
}