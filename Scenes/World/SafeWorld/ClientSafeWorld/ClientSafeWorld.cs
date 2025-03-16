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
		
		//TODO удалить целиком после теста генератора, в BattleWorld уже реализовано
		MapGenerator mapGenerator = new MapGenerator();
		foreach (var location in mapGenerator.Generate())
		{
			foreach (var entity in location.Entities)
			{
				OnStaticEntitySpawnPacket(new SC_StaticEntitySpawnPacket(entity.Type, entity.Position, entity.Scale, entity.Rotation ,entity.Color));
			}
			foreach (var borderPoint in location.GetBorderCoordinates())
			{
				OnStaticEntitySpawnPacket(new SC_StaticEntitySpawnPacket(SC_StaticEntitySpawnPacket.StaticEntityType.Wall, borderPoint, new Vector2(0.1f, 0.1f), 0, new Color(1, 0, 0)));
			}
		}
	}
}
