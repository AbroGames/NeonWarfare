using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Networking;
using NeonWarfare.Net;
using NeonWarfare.NetOld.Server;
using Server = NeonWarfare.NetOld.Server.Server;

namespace NeonWarfare;

public partial class ServerRoot : Root
{
	
	[Export] [NotNull] public Console Console { get; private set; }
	
	public Server Server { get; private set; }

	public override void _Ready()
	{
		base._Ready();
		NotNullChecker.CheckProperties(this);
	}
	
	protected override void Init()
	{
		base.Init();
		
		if (OS.GetCmdlineArgs().Contains(ServerParams.RenderFlag))
		{
			Console.QueueFree();
		}
		else
		{
			Log.AddLogger(Console);
		}
	}

	protected override void Start()
	{
		GetWindow().Set("position", new Vector2I(
			DisplayServer.ScreenGetSize().X - (int)Root.Instance.GetViewport().GetVisibleRect().Size.X,
			DisplayServer.ScreenGetSize().Y - (int)Root.Instance.GetViewport().GetVisibleRect().Size.Y - 40));

		ServerParams serverParams = ServerCmdService.GetServerParams();
		Error error = Netplay.SetServer(serverParams.Port);
		if (error == Error.Ok)
		{
			Log.Info($"Dedicated server successfully created.");
			Server = new Server(serverParams);
			AddChild(Server);
		}
		else
		{
			Log.Error($"Dedicated server created with result: {error}");
		}
	}
}