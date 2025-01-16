using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Networking;
using NeonWarfare.Utils;

namespace NeonWarfare.Scenes.Root.ServerRoot;

public partial class ServerRoot : Node2D
{
	[Export] [NotNull] public ServerPackedScenesContainer PackedScenes { get; private set; }
	[Export] [NotNull] public Console Console { get; private set; }
	
	public ServerParams CmdParams { get; private set; }

	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		Callable.From(() => { Init(); Start(); }).CallDeferred();
	}
	
	protected void Init()
	{
		RootService.CommonInit(ListenerSide.Server);
		CmdParams = ServerParams.GetFromCmd();
		
		if (CmdParams.IsRender)
		{
			Console.QueueFree();
		}
		else
		{
			Log.AddLogger(Console);
		}
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
