using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events.Global;
using KludgeBox.Networking;
using NeonWarfare.Net;
using NeonWarfare.NetOld.Server;
using NeonWarfare.Utils;
using Server = NeonWarfare.NetOld.Server.Server;

namespace NeonWarfare;

public partial class ServerRoot : Node2D
{
	[Export] [NotNull] public PackedScenesContainer PackedScenes { get; private set; }
	
	[Export] [NotNull] public Console Console { get; private set; }
	
	public Server Server { get; private set; }

	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		Callable.From(() => { Init(); Start(); }).CallDeferred();
	}
	
	protected void Init()
	{
		RootService.CommonInit(GetTree().GetMultiplayer() as SceneMultiplayer);
		
		if (CmdArgsService.ContainsInCmdArgs(ServerParams.RenderFlag))
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

		ServerParams serverParams = NetworkService.CreateServer();
		Server = new Server(serverParams);
		AddChild(Server);
	}
	
	public void Shutdown()
	{
		GetTree().Quit();
	}
}