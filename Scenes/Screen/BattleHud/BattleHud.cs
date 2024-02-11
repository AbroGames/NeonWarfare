using Godot;


public partial class BattleHud : Control
{
	[Export] [NotNull] public ProgressBar Xp { get; private set; }
	[Export] [NotNull] public TwoColoredBar HpBar { get; private set; }
	[Export] [NotNull] public Label XpLabel { get; private set; }
	[Export] [NotNull] public Label Waves { get; private set; }
	[Export] [NotNull] public Label Enemies { get; private set; }
	[Export] [NotNull] public Label Level { get; private set; }
	[Export] [NotNull] public Label WaveMessage { get; private set; }
	[Export] [NotNull] public Label Fps { get; private set; }
	[Export] [NotNull] public Label Tps { get; private set; }
	
	[Export] [NotNull] public Sprite2D TimerSprite { get; private set; }
	[Export] [NotNull] public Label TimerLabel { get; private set; }
	
	public BattleWorld BattleWorld { get; set; }
	public Vector2 WaveMessageInitialPosition { get; set; }
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		Root.Instance.EventBus.Publish(new BattleHudReadyEvent(this));
	}

	public override void _Process(double delta)
	{
		Root.Instance.EventBus.Publish(new BattleHudProcessEvent(this, delta));
	}

	public override void _PhysicsProcess(double delta)
	{
		Root.Instance.EventBus.Publish(new BattleHudPhysicsProcessEvent(this, delta));
	}
}
