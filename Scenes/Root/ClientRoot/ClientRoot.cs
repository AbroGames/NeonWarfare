using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Networking;
using NeonWarfare.Net;
using NeonWarfare.Utils;

namespace NeonWarfare;

public partial class ClientRoot : Node2D
{
	[Export] [NotNull] public PackedScenesContainer PackedScenes { get; private set; }
	[Export] [NotNull] public WorldEnvironment Environment { get; private set; }
	[Export] [NotNull] public PlayerSettings PlayerSettings { get; private set; }
	
	public ClientParams CmdParams { get; private set; }

	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		Callable.From(() => { Init(); Start(); }).CallDeferred();
	}
	
	protected void Init()
	{
		RootService.CommonInit(ListenerSide.Client);
		CmdParams = ClientParams.GetFromCmd();
		
		PlayerSettings.Init();
	}
	
	protected void Start()
	{
		if (CmdParams.AutoTest)
		{
			int serverPid = NetworkService.StartNewDedicatedServerApplication(Network.DefaultPort, PlayerSettings.PlayerName, true);
			CreateClientGame(Network.DefaultHost, Network.DefaultPort, serverPid);
		}
		else
		{
			CreateMainMenu();
		}
	}
	
	public void Shutdown()
	{
		GetTree().Quit();
	}
}