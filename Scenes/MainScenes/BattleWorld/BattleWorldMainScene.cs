using Godot;
using KludgeBox;
using KludgeBox.Events.Global;

namespace NeoVector;

public partial class BattleWorldMainScene : Node2D
{
	
	[Export] [NotNull] public BattleWorld World { get; private set; }
	[Export] [NotNull] public BattleHud Hud { get; private set; }
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);

		Hud.BattleWorld = World; //TODO BattleWorld и BattleHud линковать через BattleWorldMainScene?
		World.BattleHud = Hud; //TODO BattleWorld и SafeWorld наследовать от общего класс, BattleHud и SafeHud аналогично, мб BattleWorldMainScene и SafeWorldMainScene
	}
}