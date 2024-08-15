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
	public NetworkEntityManager NetworkEntityManager { get; private set; } = new();
	
	public Server Server { get; private set; } //TODO в game? Или куда-то ещё? Или удалить, т.к. ServerGame = Server? Но лучше не удалять, т.к. ServerGame и ClientGame должны быть схожи

	public void InitNetwork()
	{
		Network = new();
		AddChild(Network);
		Network.Initialize(GetTree().GetMultiplayer() as SceneMultiplayer);
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
		
		Server = new Server();
		AddChild(Server);
	}
}
