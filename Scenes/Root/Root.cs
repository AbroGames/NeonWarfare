using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using Godot;
using Scenes.World.Entities.Character.Player;

public partial class Root : Node2D
{
	
	[Export] [NotNull] public PackedScenesContainer PackedScenes { get; private set; }
	[Export] [NotNull] public AbroDraft.Game Game { get; private set; }
	[Export] [NotNull] public WorldEnvironment Environment { get; private set; }
	
	public EventBus EventBus { get; private set; } = new();
	public List<Object> Services { get; private set; } = new();
	
	public static Root Instance { get; private set; }

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		ServicesInit();
	}

	//Todo вынести в другое место (автоматически через аннотации, например. Хранить сервисы тоже в другом месте, наверн)
	public void ServicesInit()
	{
		PlayerXpService playerXpService = new PlayerXpService();
		
		Services.Add(playerXpService);
		
		Services.Add(new PlayerRotateService());
		Services.Add(new PlayerMovementService());
		Services.Add(new EnemyMovementService());
		Services.Add(new EnemyAttackService());
		Services.Add(new EnemyRotateService());
		Services.Add(new CameraService());
		Services.Add(new TwoColoredBarService());
		Services.Add(new BattleWorldService());
		Services.Add(new BattleWorldMainSceneService());
		Services.Add(new BattleHudWaveService());
		Services.Add(new BattleWorldWavesService());
		Services.Add(new SafeWorldService());
		Services.Add(new SafeWorldMainSceneService());
		Services.Add(new BattleWorldEnemySpawnService());
		Services.Add(new PlayerBasicSkillService());
		
		Services.Add(new BattleHudService(playerXpService));
		Services.Add(new SafeHudService(playerXpService));
	}
}
