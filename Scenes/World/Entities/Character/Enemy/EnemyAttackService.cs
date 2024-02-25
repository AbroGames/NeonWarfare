using AbroDraft.Scripts.Content;
using AbroDraft.Scripts.EventBus;
using AbroDraft.Scripts.Utils;
using Godot;

namespace AbroDraft.Scenes.World.Entities.Character.Enemy;

[GameService]
public class EnemyAttackService
{
    public EnemyAttackService()
    {
        EventBus.Subscribe<EnemyProcessEvent>(OnEnemyProcessEvent);
    }
    
    public void OnEnemyProcessEvent(EnemyProcessEvent enemyProcessEvent) {
        TryAttack(enemyProcessEvent.Enemy, enemyProcessEvent.Delta);
    }
    
    public void TryAttack(Enemy enemy, double delta)
    {
        enemy.SecToNextAttack -= delta;
        if (enemy.SecToNextAttack > 0) return;
        
        if (CanSeePlayer(enemy)) Attack(enemy);
    }
    
    public void Attack(Enemy enemy)
    {
        enemy.SecToNextAttack = 1.0 / enemy.AttackSpeed;
		
        // Создание снаряда
        Bullet.Bullet bullet = Root.Root.Instance.PackedScenes.World.Bullet.Instantiate() as Bullet.Bullet;
        // Установка начальной позиции снаряда
        bullet.GlobalPosition = enemy.GlobalPosition;
        // Установка направления движения снаряда
        bullet.Rotation = enemy.Rotation;
        bullet.Author = Bullet.Bullet.AuthorEnum.ENEMY;
        bullet.Source = enemy;
        bullet.RemainingDamage = enemy.Damage;
        if (enemy.Damage > 1000)
        {
            bullet.Transform = bullet.Transform.ScaledLocal(Vec(Mathf.Log(enemy.Damage/1000)));
        }
		
        Audio2D.PlaySoundAt(Sfx.SmallLaserShot, enemy.Position, 0.7f);
        enemy.GetParent().AddChild(bullet); //TODO refactor
    }
    
    public bool CanSeePlayer(Enemy enemy)
    {
        var collider = enemy.RayCast.GetCollider();
        return collider is Player.Player;
    }
    
}