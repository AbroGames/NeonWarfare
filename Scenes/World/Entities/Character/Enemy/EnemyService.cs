using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;

namespace NeonWarfare;

[GameService]
public class EnemyService
{
    [EventListener]
    public void OnEnemyReady(EnemyReadyEvent e)
    {
        var enemy = e.Enemy;

        enemy.PrimaryCd.Duration = 1;
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