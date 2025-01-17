using Godot;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.World.SafeWorld.ServerSafeWorld;

namespace NeonWarfare.Scenes.Game.ServerGame;

public partial class ServerGame : Node2D
{
	
	public override void _Ready()
	{
		InitNetwork();
		ServerSafeWorld serverSafeWorld = ServerRoot.Instance.PackedScenes.SafeWorld.Instantiate<ServerSafeWorld>();
		ChangeMainScene(serverSafeWorld);
	}
}
