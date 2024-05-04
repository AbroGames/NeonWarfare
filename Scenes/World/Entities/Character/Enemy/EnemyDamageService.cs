﻿using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Net;
using NeoVector;

namespace NeonWarfare;

[GameService]
public class EnemyDamageService
{
    [EventListener(ListenerSide.Server)]
    public void OnEnemyDeath(EnemyDeathEvent e)
    {
        var enemy = e.Enemy;
        var battleWorld = enemy.GetParent() as BattleWorld;
        
        battleWorld.Enemies.Remove(enemy);
        if (enemy.IsAttractor)
        {
            EventBus.Publish(new EnemyStopAttractionEvent(battleWorld, enemy));
        }
        
        long nid = Root.Instance.NetworkEntityManager.RemoveEntity(enemy);
        NetworkOld.SendPacketToClients(new ServerDestroyEntityPacket(nid));
    }
}