using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Networking;
using NeonWarfare.Utils;

namespace NeonWarfare;

public partial class ClientRoot : Node2D
{
	[Export] [NotNull] public ClientPackedScenesContainer PackedScenes { get; private set; }
	[Export] [NotNull] public WorldEnvironment Environment { get; private set; }
	
	public ClientParams CmdParams { get; private set; }
	public PlayerSettings PlayerSettings { get; private set; }

	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		Callable.From(() => { Init(); Start(); }).CallDeferred();
	}
	
	protected void Init()
	{
		RootService.CommonInit(ListenerSide.Client);
		CmdParams = ClientParams.GetFromCmd();

		PlayerSettings = PlayerSettingsService.LoadSettings();
	}
	
	protected void Start()
	{
		if (CmdParams.FastTest.HasValue)
		{
			int serverPid = ProcessesService.StartNewDedicatedServerApplication(Network.DefaultPort, PlayerSettings.PlayerName, true);
			for (int i = 0; i < CmdParams.FastTest-1; i++)
			{
				ProcessesService.StartNewClientApplicationAndAutoConnect(Network.DefaultHost, Network.DefaultPort);
			}
			
			CreateClientGame(Network.DefaultHost, Network.DefaultPort, serverPid);
		} 
		else if (CmdParams.AutoConnectIp != null)
		{
			int autoConnectPort = CmdParams.AutoConnectPort.GetValueOrDefault(Network.DefaultPort);
			CreateClientGame(CmdParams.AutoConnectIp, autoConnectPort);
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