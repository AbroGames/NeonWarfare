using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Networking;

namespace NeonWarfare;

public static class EnemyAttackService
{
    
    [EventListener(ListenerSide.Server)]
    public static void OnEnemyProcessEvent(EnemyProcessEvent enemyProcessEvent)
    {
        var (enemy, delta) = enemyProcessEvent;
        enemy.PrimaryCd.Update(delta);
        if (enemy.PrimaryCd.Use() && CanSeePlayer(enemy))
        {
            EventBus.Publish(new EnemyAttackEvent(enemy));
        }
    }
    
    [EventListener(ListenerSide.Server)]
    public static void OnEnemyAttackEvent(EnemyAttackEvent enemyAttackEvent)
    {
        Enemy enemy = enemyAttackEvent.Enemy;
		
        // Создание снаряда
        Bullet bullet = ServerRoot.Instance.PackedScenes.World.Bullet.Instantiate<Bullet>();
        // Установка начальной позиции снаряда
        bullet.GlobalPosition = enemy.GlobalPosition;
        // Установка направления движения снаряда
        bullet.Rotation = enemy.Rotation + Mathf.DegToRad(Mathf.Clamp(Rand.Gaussian(0, 5), -20, 20));
        bullet.Author = Bullet.AuthorEnum.ENEMY;
        bullet.Source = enemy;
        bullet.RemainingDamage = enemy.Damage;
        if (enemy.Damage > 1000)
        {
            bullet.Transform = bullet.Transform.ScaledLocal(Vec(Mathf.Log(enemy.Damage/1000)));
        }
		
        Audio2D.PlaySoundAt(Sfx.SmallLaserShot, enemy.Position, 0.7f);
        enemy.GetParent().AddChild(bullet); //TODO refactor (и поискать все другие места, где используется GetParent().AddChild и просто GetParent
        long nid = ServerRoot.Instance.Game.NetworkEntityManager.AddEntity(bullet);
        
        Netplay.SendToAll(new ServerSpawnEnemyBulletPacket(nid, bullet.Position.X, bullet.Position.Y, bullet.Rotation, enemy.Damage > 1000));
    }

    [EventListener(ListenerSide.Client)]
    public static void OnServerSpawnEnemyBulletPacket(ServerSpawnEnemyBulletPacket serverSpawnEnemyBulletPacket)
    {
        // Создание снаряда
        Bullet bullet = ClientRoot.Instance.PackedScenes.World.Bullet.Instantiate<Bullet>();
        bullet.Author = Bullet.AuthorEnum.ENEMY;
        bullet.Position = Vec(serverSpawnEnemyBulletPacket.X, serverSpawnEnemyBulletPacket.Y);
        bullet.Rotation = serverSpawnEnemyBulletPacket.Dir;
        if (serverSpawnEnemyBulletPacket.IsBoss)
        {
            bullet.Transform = bullet.Transform.ScaledLocal(Vec(Mathf.Log(5)));
        }

        ClientRoot.Instance.Game.MainScene.World.AddChild(bullet);
        ClientRoot.Instance.Game.NetworkEntityManager.AddEntity(bullet, serverSpawnEnemyBulletPacket.Nid);
    }
    
    
    private static bool CanSeePlayer(Enemy enemy)
    {
        var collider = enemy.RayCast.GetCollider();
        return collider is Player;
    }
    
}