using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Net;

namespace NeonWarfare;

[GameService]
public class EnemyAttackService
{
    
    [EventListener(ListenerSide.Server)]
    public void OnEnemyProcessEvent(EnemyProcessEvent enemyProcessEvent)
    {
        var (enemy, delta) = enemyProcessEvent;
        enemy.PrimaryCd.Update(delta);
        if (enemy.PrimaryCd.Use() && CanSeePlayer(enemy))
        {
            EventBus.Publish(new EnemyAttackEvent(enemy));
        }
    }
    
    [EventListener(ListenerSide.Server)]
    public void OnEnemyAttackEvent(EnemyAttackEvent enemyAttackEvent)
    {
        Enemy enemy = enemyAttackEvent.Enemy;
		
        // Создание снаряда
        Bullet bullet = Root.Instance.PackedScenes.World.Bullet.Instantiate() as Bullet;
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
        long nid = Root.Instance.NetworkEntityManager.AddEntity(bullet);
        
        NetworkOld.SendPacketToClients(new ServerSpawnEnemyBulletPacket(nid, bullet.Position.X, bullet.Position.Y, bullet.Rotation, enemy.Damage > 1000));
    }

    [EventListener]
    public void OnServerSpawnEnemyBulletPacket(ServerSpawnEnemyBulletPacket serverSpawnEnemyBulletPacket)
    {
        // Создание снаряда
        Bullet bullet = Root.Instance.PackedScenes.World.Bullet.Instantiate() as Bullet;
        bullet.Author = Bullet.AuthorEnum.ENEMY;
        bullet.Position = Vec(serverSpawnEnemyBulletPacket.X, serverSpawnEnemyBulletPacket.Y);
        bullet.Rotation = serverSpawnEnemyBulletPacket.Dir;
        if (serverSpawnEnemyBulletPacket.IsBoss)
        {
            bullet.Transform = bullet.Transform.ScaledLocal(Vec(Mathf.Log(5)));
        }

        Root.Instance.CurrentWorld.AddChild(bullet);
        Root.Instance.NetworkEntityManager.AddEntity(bullet, serverSpawnEnemyBulletPacket.Nid);
    }
    
    
    private bool CanSeePlayer(Enemy enemy)
    {
        var collider = enemy.RayCast.GetCollider();
        return collider is Player;
    }
    
}