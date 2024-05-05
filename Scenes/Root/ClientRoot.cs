using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class ClientRoot : Root
{
	
	[Export] [NotNull] public WorldEnvironment Environment { get; private set; }
	[Export] [NotNull] public PlayerSettings PlayerSettings { get; private set; }
	[Export] [NotNull] public ServerShutdowner ServerShutdowner { get; private set; }
	
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
	
	protected override void Init()
	{
		base.Init();
		SettingsService.Init();
		//TODO new network
		//AbstractNetwork = new NetworkClient(); //TODO new network
		//AddChild(AbstractNetwork);
		//AbstractNetwork.Init();
		
		var mainMenu = PackedScenes.Main.MainMenu;
		MainSceneContainer.ChangeStoredNode(mainMenu.Instantiate());
	}
}