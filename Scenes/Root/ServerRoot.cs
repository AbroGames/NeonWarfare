using System;
using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Net;
using NeoVector;

namespace NeonWarfare;

public partial class ServerRoot : Root
{
	
	[Export] [NotNull] public Console Console { get; private set; }
	
	/*
	[Export] [NotNull] public NodeContainer MainSceneContainer { get; private set; }
	[Export] [NotNull] public WorldEnvironment Environment { get; private set; }
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
	
	public new static ServerRoot Instance { get; private set; }

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
		//AbstractNetwork = new NetworkServer();
		//AddChild(AbstractNetwork);
		//AbstractNetwork.Init();
		InitServerService.InitServer();
	}
}