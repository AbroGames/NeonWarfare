using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using NeonWarfare;

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
		Network.AddInstanceResolver(typeof(ClientGame), id => this);

		PingChecker = new();
		AddChild(PingChecker);
		Network.AddInstanceResolver(typeof(PingChecker), id => PingChecker);
	} 

	public void ConnectToServer(string host, int port)
	{
		Error error = Network.SetClient(host, port);
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
