using Godot;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.World.SafeWorld.ServerSafeWorld;
using NeonWarfare.Scripts.Content.GameSettings;
using NeonWarfare.Scripts.Utils.GameSettings;

namespace NeonWarfare.Scenes.Game.ServerGame;

public partial class ServerGame : Node2D
{

	public GameSettings GameSettings { get; set; } = new();
	
	public override void _Ready()
	{
		InitNetwork();
		ServerSafeWorld serverSafeWorld = ServerRoot.Instance.PackedScenes.SafeWorld.Instantiate<ServerSafeWorld>();
		ChangeMainScene(serverSafeWorld);
	}
}
