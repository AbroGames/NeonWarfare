using Godot;
using KludgeBox;
using NeonWarfare.Net;

namespace NeonWarfare;

public partial class ClientRoot
{

	//Т.к. в момент создания DedicatedServerApplication ещё не существует ноды ClientGame, то храним serverPid в ClientRoot. 
	//Но ловим нотификации и вырубаем сервер, в том числе по команде из ClientGame
    public ServerShutdowner ServerShutdowner { get; private set; }

	public void AddServerShutdowner(int serverPid)
	{
		ServerShutdowner?.QueueFree(); //Удаление ноды вызовет, в том числе удаление сервера
		ServerShutdowner = new ServerShutdowner();
		ServerShutdowner.ServerPid = serverPid;
		AddChild(ServerShutdowner);
	}
}