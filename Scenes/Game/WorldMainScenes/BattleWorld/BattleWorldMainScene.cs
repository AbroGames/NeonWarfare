using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class BattleWorldMainScene : WorldMainScene
{
	
	[Export] [NotNull] public BattleWorld BattleWorld { get; private set; }
	[Export] [NotNull] public BattleHud BattleHud { get; private set; }
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
		
		World = BattleWorld;
		Hud = BattleHud;
		
		BattleHud.BattleWorld = BattleWorld;
		BattleWorld.BattleHud = BattleHud; //TODO BattleWorld и SafeWorld наследовать от общего класс, BattleHud и SafeHud аналогично, мб BattleWorldMainScene и SafeWorldMainScene
	}
}