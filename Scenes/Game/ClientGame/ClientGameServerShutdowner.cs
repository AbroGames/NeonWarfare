using Godot;
using KludgeBox;
using NeonWarfare;

namespace NeonWarfare.Scenes.Game.ClientGame;

public partial class ClientGame
{
	
	public ProcessShutdowner ServerShutdowner { get; private set; }

	public void AddServerShutdowner(int serverPid)
	{
		ServerShutdowner?.QueueFree(); //Удаление ноды вызовет, в том числе удаление сервера
		
		ServerShutdowner = new ProcessShutdowner();
		ServerShutdowner.ProcessPid = serverPid;
		ServerShutdowner.LogMessageGenerator = pid => $"Kill server process: {pid}.";
		
		AddChild(ServerShutdowner);
	}
}
