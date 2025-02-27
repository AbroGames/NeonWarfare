using System;
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
		foreach (SC_StaticEntitySpawnPacket entitySpawnPacket in mapGenerator.Generate())
		{
			OnStaticEntitySpawnPacket(entitySpawnPacket);
		}
	}
}
