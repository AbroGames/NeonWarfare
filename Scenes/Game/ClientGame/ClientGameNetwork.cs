using Godot;
using KludgeBox;
using KludgeBox.Networking;
using NeonWarfare;
using NeonWarfare.Net;
using NeonWarfare.NetOld;

public partial class ClientGame
{
	
	public Network Network { get; private set; }
	public NetworkEntityManager NetworkEntityManager { get; private set; } = new();
	
	public void InitNetwork()
	{
		Network = new();
		Network.Initialize(GetTree().GetMultiplayer() as SceneMultiplayer);
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
	
	//TODO Вызывать здесь CloseConnection
}
