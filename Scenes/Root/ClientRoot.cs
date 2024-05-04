using System;
using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Net;

namespace NeonWarfare;

public partial class ClientRoot : Root
{
	
	/*
	[Export] [NotNull] public NodeContainer MainSceneContainer { get; private set; }
	[Export] [NotNull] public WorldEnvironment Environment { get; private set; }
	[Export] [NotNull] public Console Console { get; private set; }
	[Export] [NotNull] public PlayerSettings PlayerSettings { get; private set; }
	[Export] [NotNull] public PackedScenesContainer PackedScenes { get; private set; }

	public AbstractNetwork AbstractNetwork;
	public ServiceRegistry ServiceRegistry { get; private set; } = new();

	public Server Server { get; private set; }
	public int? ServerPid { get; set; }
	public bool IsServer => Server != null;

	public World CurrentWorld;
	public NetworkEntityManager NetworkEntityManager { get; private set; } = new();
	*/
	
	public new static ClientRoot Instance { get; private set; }

	public override void _EnterTree()
	{
		base._EnterTree();
		Instance = this;
	}

	public override void _Ready()
	{
		base._Ready();
		NotNullChecker.CheckProperties(this);
	}
	
	/*
	private void Init()
	{
		LogCmdArgs();
		NetworkOld.Init();
		SettingsService.Init();
        
		if (OS.GetCmdlineArgs().Contains(ServerParams.ServerFlag))
		{
			InitGameAsServer();
		}
		else
		{
			InitGameAsClient();
		}
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

	private void InitGameAsClient()
	{
		Console.QueueFree(); //TODO просто не создавать консоль по дефолту! Только в случае, если это сервер и нужна консоль. Можно упаковать в ConsoleContainer
		AbstractNetwork = new NetworkClient();
		AddChild(AbstractNetwork);
		AbstractNetwork.Init();
		
		var mainMenu = PackedScenes.Main.MainMenu;
		MainSceneContainer.ChangeStoredNode(mainMenu.Instantiate());
	}

	private void InitGameAsServer()
	{
		if (OS.GetCmdlineArgs().Contains(ServerParams.RenderFlag))
		{
			Console.QueueFree();
		}
		else
		{
			Log.AddLogger(Console);
		}
		
		AbstractNetwork = new NetworkServer();
		AddChild(AbstractNetwork);
		AbstractNetwork.Init();
		InitServerService.InitServer();
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
	}*/
}