using System.Linq;
using Godot;
using NeonWarfare.Scenes.PackedScenesContainer.ServerPackedScenesContainer;
using NeonWarfare.Scenes.Screen.Console;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Events;
using NeonWarfare.Scripts.Utils.CmdArgs;

namespace NeonWarfare.Scenes.Root.ServerRoot;

public partial class ServerRoot : Node2D
{
	[Export] [NotNull] public ServerPackedScenesContainer PackedScenes { get; private set; }
	[Export] [NotNull] public ServerGui ServerGui { get; private set; }
	
	public ServerParams CmdParams { get; private set; }

	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		Callable.From(() => { Init(); Start(); }).CallDeferred();
		GetTree().Root.Title = $"[SERVER] {GetTree().Root.Title}";
	}
	
	protected void Init()
	{
		RootService.CommonInit(ListenerSide.Server);
		CmdParams = ServerParams.GetFromCmd();
	}

	protected void Start()
	{
		CreateServerGame(CmdParams.Port, CmdParams.ParentPid);
	}
	
	public void Shutdown()
	{
		GetTree().Quit();
	}
}
