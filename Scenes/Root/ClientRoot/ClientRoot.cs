using Godot;
using NeonWarfare.Scenes.PackedScenesContainer.ClientPackedScenesContainer;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Events;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.Utils.CmdArgs;
using NeonWarfare.Scripts.Utils.PlayerSettings;

namespace NeonWarfare.Scenes.Root.ClientRoot;

public partial class ClientRoot : Node2D
{
	[Export] [NotNull] public ClientPackedScenesContainer PackedScenes { get; private set; }
	
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
		if (CmdParams.AutoConnectIp != null)
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
