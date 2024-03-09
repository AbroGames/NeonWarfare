using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;

namespace NeoVector;

[GameService]
public class EnemyService
{
    [EventListener]
    public void OnEnemyReady(EnemyReadyEvent e)
    {
        var enemy = e.Enemy;

        enemy.PrimaryCd.Duration = 1;
        enemy.PrimaryCd.Ready += () =>
        {
            Log.Info("Enemy primary cd ready");
        };
        
        enemy.TeleportCd.Ready += () =>
        {
            EventBus.Publish(new EnemyAboutToTeleportEvent(enemy));
        };
    }

    [EventListener]
    public void OnEnemyProcessEvent(EnemyProcessEvent e)
    {
        var (enemy, delta) = e;
        enemy.TeleportCd.Update(delta);
    }
}