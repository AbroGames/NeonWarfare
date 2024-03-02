using KludgeBox;
using KludgeBox.Events;

namespace NeoVector.World;

[GameService]
public class EnemyDamageService
{
    [EventListener]
    public void OnEnemyDeath(EnemyDeathEvent e)
    {
        var enemy = e.Enemy;
        var battleWorld = enemy.GetParent() as BattleWorld;
        
        battleWorld.Enemies.Remove(enemy);
        if (enemy.IsAttractor)
        {
            EventBus.Publish(new EnemyStopAttractionEvent(enemy));
        }
    }
}