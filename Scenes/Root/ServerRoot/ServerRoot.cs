using System.Linq;
using Godot;
using KludgeBox;
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
		
		//TODO new network
		AbstractNetwork = new ServerNetwork();
		AddChild(AbstractNetwork);
		AbstractNetwork.Init();
		
		//InitServerService.InitServer(); //TODO old network
	}
	
	public void AddServer(Server server)
	{
		Server = server;
		AddChild(server);
	}
}