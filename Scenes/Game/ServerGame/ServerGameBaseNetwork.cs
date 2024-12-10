using System.Collections.Generic;
using Godot;
using KludgeBox;
using KludgeBox.Networking;
using NeonWarfare;
using NeonWarfare.Net;
using NeonWarfare.NetOld;
using NeonWarfare.NetOld.Server;

public partial class ServerGame
{
	
	public Network Network { get; private set; }
	public IDictionary<long, PlayerServerInfo> PlayerServerInfo { get; private set; } = new Dictionary<long, PlayerServerInfo>();

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
			Log.Info($"Network successfully created.");
		}
		else
		{
			Log.Error($"Create network with result: {error}");
		}
	}
}
