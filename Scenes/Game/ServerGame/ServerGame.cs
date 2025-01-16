using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using NeonWarfare;

namespace NeonWarfare.Scenes.Game.ServerGame;

public partial class ServerGame : Node2D
{
	
	public override void _Ready()
	{
		InitNetwork();
		ChangeMainScene(new ServerSafeWorld());
	}
}
