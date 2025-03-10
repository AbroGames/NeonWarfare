using System;
using System.Collections.Generic;
using Godot;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.Utils.MapGenerator;

namespace NeonWarfare.Scenes.World.SafeWorld.ClientSafeWorld;

public partial class ClientSafeWorld : ClientWorld
{
	
	public override void _Ready()
	{
		base._Ready();
		PlaySafeMusic();
		
		//TODO перенести на серверную сторону, и в боевой мир вместо мирного хаба
		MapGenerator mapGenerator = new MapGenerator();
		foreach (SC_LocationSpawnPacket locationSpawnPacket in mapGenerator.Generate())
		{
			foreach (SC_StaticEntitySpawnPacket entitySpawnPacket in locationSpawnPacket.Entities)
			{
				OnStaticEntitySpawnPacket(entitySpawnPacket);
			}

			foreach (var borderPoint in locationSpawnPacket.GetBorderCoordinates())
			{
				OnStaticEntitySpawnPacket(new SC_StaticEntitySpawnPacket(SC_StaticEntitySpawnPacket.StaticEntityType.Wall, borderPoint, new Vector2(0.1f, 0.1f), 0, new Color(1, 0, 0)));
			}
		}
	}
}
