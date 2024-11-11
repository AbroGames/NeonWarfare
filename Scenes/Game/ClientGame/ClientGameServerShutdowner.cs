using Godot;
using KludgeBox;
using NeonWarfare;
using NeonWarfare.NetOld;

public partial class ClientGame
{
	
	public ProcessShutdowner ServerShutdowner { get; private set; }

	public void AddServerShutdowner(int serverPid)
	{
		ServerShutdowner?.QueueFree(); //Удаление ноды вызовет, в том числе удаление сервера
		ServerShutdowner = new ProcessShutdowner();
		ServerShutdowner.ProcessPid = serverPid;
		ServerShutdowner.LogMessage = "Kill server process.";
		AddChild(ServerShutdowner);
	}
}
