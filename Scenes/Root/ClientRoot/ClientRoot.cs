using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class ClientRoot : Root
{
	
	[Export] [NotNull] public WorldEnvironment Environment { get; private set; }
	[Export] [NotNull] public PlayerSettings PlayerSettings { get; private set; }
	
	//TODO можно создать сервер несколько раз за игру. Лучше привязать ServerShutdowner куда-то в Network и при каждом запуске сервра или подключение по сети пересоздавать объект Network
	//TODO таким образом можно по дефолту вообще Network не инициализировать, а делать это только при подключение
	//TODO в идеале всю эту логику поместить в финалайзер (метод Free) ноды Game или подобной, которая общая для BattleWorld и SafeWorld, но не используется в MainMenu
	//TODO и переместить в другое место сам файл ServerSutdowner
    public ServerShutdowner ServerShutdowner { get; private set; } //TODO ServerShutdowner в отдельный файл (partial class)

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

	public void AddServerShutdowner(int serverPid)
	{
		ServerShutdowner = new ServerShutdowner();
		ServerShutdowner.ServerPid = serverPid;
		AddChild(ServerShutdowner);
	}
}