using System;
using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Net;

namespace NeoVector;

public partial class Root : Node2D
{
	
	[Export] [NotNull] public PackedScenesContainer PackedScenes { get; private set; }
	[Export] [NotNull] public Game Game { get; private set; }
	[Export] [NotNull] public WorldEnvironment Environment { get; private set; }
	
	public ServiceRegistry ServiceRegistry { get; private set; } = new();

	public Server Server { get; private set; }
	public int? ServerPid { get; set; }
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
		ServicesInit();
		PacketRegistry.ScanPackets();
		
		Callable.From(() =>
		{
			EventBus.Publish(new RootInitEvent());
		}).CallDeferred();
	}

	public override void _Process(double delta)
	{
		EventBus.Publish(new RootProcessEvent(this, delta));
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

	public override void _Notification(int id)
	{
		long[] serverShutdownNotificationTypes =
		[
			NotificationWMCloseRequest, NotificationCrash, NotificationDisabled, NotificationPredelete,
			NotificationExitTree
		];

		if (ServerPid.HasValue && serverShutdownNotificationTypes.Contains(id) && OS.IsProcessRunning(ServerPid.Value))
		{
			Log.Info($"Kill server process. Pid: {ServerPid.Value}");
			OS.Kill(ServerPid.Value);
		}
	}
}