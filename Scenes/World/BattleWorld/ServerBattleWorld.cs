using System.Collections.Generic;
using KludgeBox;
using KludgeBox.Events.Global;

namespace NeonWarfare;

public partial class ServerBattleWorld : ServerWorld
{
	public EnemyWave EnemyWave { get; set; } = new();
	public readonly ISet<Enemy> Enemies = new HashSet<Enemy>();
	public readonly ISet<Enemy> EnemyAttractors = new HashSet<Enemy>();

	public override void _Process(double delta)
	{
		base._Process(delta);
		EventBus.Publish(new BattleWorldProcessEvent(this, delta));
	}
}