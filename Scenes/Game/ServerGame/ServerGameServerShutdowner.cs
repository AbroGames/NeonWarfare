using Godot;
using KludgeBox;
using NeonWarfare;
using NeonWarfare.NetOld;

public partial class ServerGame
{
	
	public ProcessDeadChecker ClientDeadChecker { get; private set; }

	public void AddClientDeadChecker(int clientPid)
	{
		ClientDeadChecker?.QueueFree();
		
		ClientDeadChecker = new ProcessDeadChecker();
		ClientDeadChecker.ProcessPid = clientPid;
		ClientDeadChecker.LogMessageGenerator = pid => $"Parent process {pid} is dead. Shutdown server.";
		ClientDeadChecker.ActionWhenDead += () => ServerRoot.Instance.Shutdown();
		
		AddChild(ClientDeadChecker);
	}
}
