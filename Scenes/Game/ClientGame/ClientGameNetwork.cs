using Godot;
using KludgeBox;
using NeonWarfare;
using NeonWarfare.Net;
using NeonWarfare.NetOld;

public partial class ClientGame
{
	
	public NetworkEntityManager NetworkEntityManager { get; private set; } = new();

	public void ConnectToServer(string host, int port)
	{
		NetworkService.ConnectToServer(host, port);
	}
}
