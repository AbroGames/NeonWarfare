using Godot;
using KludgeBox;
using KludgeBox.Networking;
using NeonWarfare;
using NeonWarfare.Net;
using NeonWarfare.NetOld;
using NeonWarfare.NetOld.Server;

public partial class ServerGame
{
	public NetworkEntityManager NetworkEntityManager { get; private set; } = new();
	
	public Server Server { get; private set; } //TODO в game? Или куда-то ещё? Или удалить, т.к. ServerGame = Server? Но лучше не удалять, т.к. ServerGame и ClientGame должны быть схожи

	public void InitNetwork()
	{
		Netplay.Initialize(GetTree().GetMultiplayer() as SceneMultiplayer);
	}

	public void CreateServer(int port)
	{
		NetworkService.CreateServer(port);
		Server = new Server();
		AddChild(Server);
	}
}
