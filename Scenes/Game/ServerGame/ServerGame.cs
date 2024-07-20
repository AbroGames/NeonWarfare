using Godot;
using System;
using KludgeBox;
using NeonWarfare;

public partial class ServerGame : Game
{
	
	public override void _Ready()
	{
		base._Ready();
		var safeWorld = ServerRoot.Instance.PackedScenes.Main.SafeWorld;
		ChangeMainScene(safeWorld.Instantiate<SafeWorldMainScene>());
	}

	public override void _Process(double delta)
	{
		
	}
}
