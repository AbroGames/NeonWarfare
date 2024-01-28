using Godot;
using System;
using AbroDraft.Net;

public partial class Servir : Button
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Pressed += () =>
		{
			Network.CreateServer(8800);
		};
	}
}
