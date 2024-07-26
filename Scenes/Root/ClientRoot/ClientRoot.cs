using Godot;
using KludgeBox;
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

	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		Callable.From(() => { Init(); Start(); }).CallDeferred();
	}
	
	protected void Init()
	{
		CmdArgsService.LogCmdArgs();
		EventBus.Init();
		Netplay.Initialize(GetTree().GetMultiplayer() as SceneMultiplayer);
		
		SettingsService.Init();
	}
	
	protected void Start()
	{
		MenuService.ActivateMainMenu();
	}
	
	public void Shutdown()
	{
		GetTree().Quit();
	}
}