using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using NeonWarfare;
using NeonWarfare.Net;
using NeonWarfare.NetOld;

public partial class ClientGame
{
	
	public Network Network { get; private set; }
	public PingChecker PingChecker { get; private set; }
	
	public void InitNetwork()
	{
		Network = new();
		AddChild(Network);
		Network.Initialize(GetTree().GetMultiplayer() as SceneMultiplayer);
		Network.SetDefaultResolver(nid => World.NetworkEntityManager.GetNode((long) nid));

		PingChecker = new();
		AddChild(PingChecker);
	} 

	public void ConnectToServer(string host, int port)
	{
		Error error = Network.SetClient(host, port);
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
