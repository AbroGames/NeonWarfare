using Godot;
using KludgeBox;
using KludgeBox.Events.Global;

namespace NeoVector;

public partial class SafeHud : Control
{
	[Export] [NotNull] public TwoColoredBar HpBar { get; private set; }
	[Export] [NotNull] public Label Level { get; private set; }
	[Export] [NotNull] public Label Fps { get; private set; }
	
	public SafeWorld SafeWorld { get; set; }
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
	}

	public override void _Process(double delta)
	{
		Player player = SafeWorld.Player;
		if (player == null) return;

		HpBar.CurrentUpperValue = player.Hp;
		HpBar.CurrentLowerValue = player.Hp; //TODO сделать аналогично с BattleHud, вынести в общий родительский класс (не дублировать код)
		HpBar.MaxValue = player.MaxHp;
		HpBar.Label.Text = $"Health: {player.Hp:N0} / {player.MaxHp:N0}";
		Fps.Text = $"FPS: {Engine.GetFramesPerSecond():N0}";
	}
}