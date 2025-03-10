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
		foreach (MapGenerator.Location location in mapGenerator.Generate())
		{
			foreach (MapGenerator.Entity entity in location.Entities)
			{
				OnStaticEntitySpawnPacket(new SC_StaticEntitySpawnPacket(SC_StaticEntitySpawnPacket.StaticEntityType.Wall, entity.Position, entity.Scale, entity.Rotation ,entity.Color));
			}
			//TODO потом удалю, чисто для отображения точек границ
			foreach (var borderPoint in location.GetBorderCoordinates())
			{
				OnStaticEntitySpawnPacket(new SC_StaticEntitySpawnPacket(SC_StaticEntitySpawnPacket.StaticEntityType.Wall, borderPoint, new Vector2(0.1f, 0.1f), 0, new Color(1, 0, 0)));
			}
		}
	}
}
