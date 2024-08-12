using Godot;
using KludgeBox;
using KludgeBox.Networking;
using NeonWarfare;
using NeonWarfare.Net;
using NeonWarfare.NetOld;

public partial class ClientGame
{
	
	public NetworkEntityManager NetworkEntityManager { get; private set; } = new();
	
	public void InitNetwork()
	{
		Network.Initialize(GetTree().GetMultiplayer() as SceneMultiplayer);
	} 

	public void ConnectToServer(string host, int port)
	{
		NetworkService.ConnectToServer(host, port);
	}
	
	//TODO перенести сюда Network, а так же ServerShutdowner
}
