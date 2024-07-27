using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using NeonWarfare;
using NeonWarfare.NetOld;
using NeonWarfare.NetOld.Server;

public partial class ServerGame : Node2D
{

    private static ServerGame Instance => ServerRoot.Instance.Game;
	
	public override void _Ready()
	{
		InitNetwork();
		ChangeMainScene(new ServerSafeWorld());
	}
}
