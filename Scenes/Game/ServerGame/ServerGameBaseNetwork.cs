using System.Collections.Generic;
using Godot;
using KludgeBox;
using KludgeBox.Networking;
using NeonWarfare;

public partial class ServerGame
{
	
	public Network Network { get; private set; }

	public void InitNetwork()
	{
		Network = new();
		AddChild(Network);
		Network.Initialize(GetTree().GetMultiplayer() as SceneMultiplayer);
		Network.SetDefaultResolver(nid => World.NetworkEntityManager.GetNode((long) nid));
		Network.AddInstanceResolver(typeof(ServerGame), id => this);
	}

	public void CreateServer(int port)
	{
		Error error = Network.SetServer(port);
		if (error == Error.Ok)
		{
			Log.Info("Create network successfully.");
		}
		else
		{
			Log.Error($"Create network with result: {error}");
		}
	}
}
