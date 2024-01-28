using Godot;
using System;
using AbroDraft.Net;
using AbroDraft.Net.Packets;

public partial class Cleent : Button
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Pressed += () =>
		{
			Network.ConnectToRemoteServer("localhost", 8800);
			Network.Api.ConnectedToServer += Greeting;
		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private static void Greeting()
	{
		Network.Api.ConnectedToServer -= Greeting;
		Network.SendPacket(new HelloPacket());
	}
}
