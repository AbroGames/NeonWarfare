using System;
using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Networking;
using NeonWarfare.NetOld;
using NeonWarfare.NetOld.Server;

namespace NeonWarfare;

public partial class Root : Node2D
{
	
	[Export] [NotNull] public NodeContainer MainSceneContainer { get; private set; }
	[Export] [NotNull] public PackedScenesContainer PackedScenes { get; private set; }
    
	public World CurrentWorld;
	public NetworkEntityManager NetworkEntityManager { get; private set; } = new();

	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		
		Callable.From(() => { Init(); Start(); }).CallDeferred();
	}

	protected virtual void Init()
	{
		LogCmdArgs();
		EventBusInit();
		Netplay.Initialize(GetTree().GetMultiplayer() as SceneMultiplayer);
	}

	protected virtual void Start() {}
	
	private void LogCmdArgs()
	{
		if (!OS.GetCmdlineArgs().IsEmpty())
		{
			Log.Info("Cmd args: " + OS.GetCmdlineArgs().Join());
		}
		else
		{
			Log.Info("Not have cmd args");
		}
	}
}