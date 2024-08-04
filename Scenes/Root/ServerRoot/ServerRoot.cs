using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
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
	
	public ServerParams CmdParams { get; private set; }
	
	public Server Server { get; private set; } //TODO в game? Или куда-то ещё? Или удалить, т.к. ServerGame = Server? Но лучше не удалять, т.к. ServerGame и ClientGame должны быть схожи

	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		Callable.From(() => { Init(); Start(); }).CallDeferred();
	}
	
	protected void Init()
	{
		RootService.CommonInit(GetTree().GetMultiplayer(), ListenerSide.Server);
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

		NetworkService.CreateServer(CmdParams.Port);
		Server = new Server();
		AddChild(Server);
	}
	
	public void Shutdown()
	{
		GetTree().Quit();
	}
}