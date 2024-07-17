using Godot;
using KludgeBox;
using NeonWarfare.Net;

namespace NeonWarfare;

public partial class ClientRoot : Root
{
	
	[Export] [NotNull] public WorldEnvironment Environment { get; private set; }
	[Export] [NotNull] public PlayerSettings PlayerSettings { get; private set; }

	public override void _Ready()
	{
		base._Ready();
		NotNullChecker.CheckProperties(this);
	}
	
	protected override void Init()
	{
		base.Init();
		SettingsService.Init();
	}
	
	protected override void Start()
	{
		var mainMenu = PackedScenes.Main.MainMenu;
		SetMainScene(mainMenu.Instantiate<MainMenuMainScene>());
	}
}