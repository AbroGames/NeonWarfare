using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Net;
using NeoVector;

namespace NeonWarfare;

public partial class Root : Node2D
{
	
	[Export] [NotNull] public NodeContainer MainSceneContainer { get; private set; }
	[Export] [NotNull] public PackedScenesContainer PackedScenes { get; private set; }

	public AbstractNetwork AbstractNetwork;
	public ServiceRegistry ServiceRegistry { get; private set; } = new();

	public Server Server { get; private set; }
	public bool IsServer => Server != null;

	public World CurrentWorld;
	public NetworkEntityManager NetworkEntityManager { get; private set; } = new();
	
	public static Root Instance { get; private set; }

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		Callable.From(Init).CallDeferred();
	}

	protected virtual void Init()
	{
		ServicesInit();
		PacketRegistry.ScanPackets();
		LogCmdArgs();
		NetworkOld.Init();
	}

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
	
	public void ServicesInit()
	{
		ServiceRegistry.RegisterServices();

		if (OS.GetCmdlineArgs().Contains(ServerParams.ServerFlag))
		{
			EventBus.Side = ListenerSide.Server;
		}
		else
		{
			EventBus.Side = ListenerSide.Client;
		}
		
		
		EventBus.RegisterListeners(ServiceRegistry);
	}

	public void AddServer(Server server)
	{
		Server = server;
		AddChild(server);
	}
}