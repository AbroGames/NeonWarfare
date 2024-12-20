using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Networking;
using NeonWarfare.Net;
using NeonWarfare.Utils;

namespace NeonWarfare;

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
		GetWindow().Set("position", new Vector2I(
			DisplayServer.ScreenGetSize().X - (int) GetViewport().GetVisibleRect().Size.X,
			DisplayServer.ScreenGetSize().Y - (int) GetViewport().GetVisibleRect().Size.Y - 40));

		CreateServerGame(CmdParams.Port, CmdParams.ParentPid);
	}
	
	public void Shutdown()
	{
		GetTree().Quit();
	}
}