using Godot;
using KludgeBox;
using KludgeBox.Events;

namespace NeoVector;

[GameService]
public class BulletService
{
    [EventListener]
    public void OnBulletReady(BulletReadyEvent e)
    {
	    var bullet = e.Bullet;
	    
		bullet.Modulate = Bullet.Colors[bullet.Author];
        		
        bullet.GetNode<Area2D>("Area2D").AreaEntered += area =>
        {
        	if(area.GetParent() is not Character body) return;
        	
        	if (body is Player player)
        	{
        		if (bullet.Author != Bullet.AuthorEnum.PLAYER)
        		{
        			double damage = bullet.RemainingDamage;
			        ApplyDamage(bullet, player, new Color(0, 0, 0));
        			player.Camera.Punch(player.Position - bullet.Position, 10, 30);
        			Audio2D.PlaySoundAt(Sfx.FuturisticHit, body.Position, 0.5f).PitchVariation(0.15f);
        			
        			var hit = Fx.CreateBulletHitFx();
        			hit.Modulate = bullet.Modulate;
        			hit.Rotation = bullet.Rotation - Mathf.Pi / 2;
        			hit.Scale =bullet.Scale;
        			hit.Position = bullet.Position;
			        bullet.GetParent().AddChild(hit);
        		}
        	}
        	
        	if (body is Enemy enemy)
        	{
        		if (bullet.Author != Bullet.AuthorEnum.ENEMY)
        		{
			        ApplyDamage(bullet, enemy,new Color(1, 0, 0));
        			Audio2D.PlaySoundAt(Sfx.Hit, body.Position, 0.5f).PitchVariation(0.25f);
        			double K = enemy.IsBoss ? 0.0025 : 0.025;
        			enemy.Position += Vector2.FromAngle(bullet.Rotation - Mathf.Pi / 2) * bullet.Speed * K;
        			
        			var hit = Fx.CreateBulletHitFx();
        			hit.Modulate = bullet.Modulate;
        			hit.Rotation = bullet.Rotation - Mathf.Pi / 2;
        			hit.Scale = bullet.Scale;
        			hit.Position = bullet.Position;
			        bullet.GetParent().AddChild(hit);
        		}
        	}
        	
        	if (body is Ally ally)
        	{
        		if (bullet.Author != Bullet.AuthorEnum.ALLY)
        		{
        			Utils.DoNothing();
        		}
        	}
        };
    }

    [EventListener]
    public void OnBulletProcess(BulletProcessEvent e)
    {
	    var(bullet, delta) = e;
	    bullet.Position += Vector2.FromAngle(bullet.Rotation - Mathf.Pi / 2) * bullet.Speed * delta;
	    bullet.RemainingDistance -= bullet.Speed * delta;
	    if (bullet.RemainingDistance <= 0)
	    {
		    bullet.QueueFree();
	    }
    }
    
    internal void ApplyDamage(Bullet bullet, Character to, Color color)
    {
	    if (bullet.RemainingDamage <= 0)
		    return;
		
	    var hp = to.Hp;
	    to.TakeDamage(new Damage(bullet.Author, color, bullet.RemainingDamage, bullet.Source));
	    bullet.RemainingDamage -= hp;
		

	    if (bullet.RemainingDamage <= 0)
	    {
		    bullet.QueueFree();
	    }
    }
}