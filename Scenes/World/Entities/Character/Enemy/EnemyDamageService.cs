using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Networking;

namespace NeonWarfare;

public static class EnemyDamageService
{
    [EventListener(ListenerSide.Server)]
    public static void OnEnemyDeath(EnemyDeathEvent e)
    {
        var enemy = e.Enemy;
        var battleWorld = enemy.GetParent() as ServerBattleWorld;
        
        battleWorld.Enemies.Remove(enemy);
        if (enemy.IsAttractor)
        {
            EventBus.Publish(new EnemyStopAttractionEvent(battleWorld, enemy));
        }
        
        long nid = ServerRoot.Instance.Game.World.NetworkEntityManager.RemoveEntity(enemy);
        Network.SendToAll(new ServerDestroyEntityPacket(nid));
    }
}