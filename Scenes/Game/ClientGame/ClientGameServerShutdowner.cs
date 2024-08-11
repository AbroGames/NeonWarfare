using Godot;
using KludgeBox;
using NeonWarfare;
using NeonWarfare.NetOld;

public partial class ClientGame
{
	
	public ServerShutdowner ServerShutdowner { get; private set; }

	public void AddServerShutdowner(int serverPid)
	{
		ServerShutdowner?.QueueFree(); //Удаление ноды вызовет, в том числе удаление сервера
		ServerShutdowner = new ServerShutdowner();
		ServerShutdowner.ServerPid = serverPid;
		AddChild(ServerShutdowner);
	}
}
