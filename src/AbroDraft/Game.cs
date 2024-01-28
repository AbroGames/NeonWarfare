using System.Runtime.CompilerServices;
using AbroDraft.Net;
using AbroDraft.Net.Packets;
using Godot;
using KludgeBox;

namespace AbroDraft;

public partial class Game : Node2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		DisplayServer.WindowSetMode(DisplayServer.WindowMode.Maximized);
		var firstScene = References.Instance.FirstSceneBlueprint;
		References.Instance.MenuContainer.ChangeStoredNode(firstScene.Instantiate() as Control);

		PacketRegistry.RegisterPacketType(typeof(HelloPacket));
		
		Network.ReceivedPacket += packet =>
		{
			Log.Info($"{Network.Mode} Received packet of type {packet}");
			if (packet is HelloPacket hp)
			{
				Log.Info($"Received Hello packet: {hp.Message}");
			}
		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}