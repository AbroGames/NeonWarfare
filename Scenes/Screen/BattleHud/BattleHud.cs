using Godot;


public partial class BattleHud : Control
{
	[ExportGroup("Bars")]
	[Export] [NotNull] public ProgressBar Xp { get; private set; }
	[Export] [NotNull] public TwoColoredBar HpBar { get; private set; }
	[Export] [NotNull] public Label XpLabel { get; private set; }
	
	
	[ExportGroup("Labels")]
	[Export] [NotNull] public Label Waves { get; private set; }
	[Export] [NotNull] public Label Enemies { get; private set; }
	[Export] [NotNull] public Label Level { get; private set; }
	
	
	[ExportGroup("FPS & TPS")]
	[Export] [NotNull] public Label Fps { get; private set; }
	[Export] [NotNull] public Label Tps { get; private set; }
	
	
	[ExportGroup("Other")]
	[Export] [NotNull] public Label WaveMessage { get; private set; }
	[Export] [NotNull] public Sprite2D TimerSprite { get; private set; }
	[Export] [NotNull] public Label TimerLabel { get; private set; }
	
	
	[ExportGroup("Abilities")]
	[Export] [NotNull] public Icon BeamIcon { get; private set; }
	[Export] [NotNull] public Icon SolarBeamIcon { get; private set; }
	
	
	public BattleWorld BattleWorld { get; set; }
	public Vector2 WaveMessageInitialPosition { get; set; }
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		EventBus.Publish(new BattleHudReadyEvent(this));
	}

	public override void _Process(double delta)
	{
		EventBus.Publish(new BattleHudProcessEvent(this, delta));
	}

	public override void _PhysicsProcess(double delta)
	{
		EventBus.Publish(new BattleHudPhysicsProcessEvent(this, delta));
	}
}
