using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using NeonWarfare;

public partial class ServerGame : Node2D
{
	
	public override void _Ready()
	{
		InitNetwork();
		ChangeMainScene(new ServerSafeWorld());
	}
}
