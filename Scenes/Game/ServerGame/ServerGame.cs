using Godot;
using NeonWarfare.Scenes.World.SafeWorld.ServerSafeWorld;

namespace NeonWarfare.Scenes.Game.ServerGame;

public partial class ServerGame : Node2D
{
	
	public override void _Ready()
	{
		InitNetwork();
		ChangeMainScene(new ServerSafeWorld());
	}
}
