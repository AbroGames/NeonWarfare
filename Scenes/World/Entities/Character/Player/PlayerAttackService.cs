using Godot;
using KludgeBox;
using KludgeBox.Events;

namespace NeoVector.World;

[GameService]
public class PlayerAttackService
{
    [EventListener]
    public void OnPlayerAttackPrimary(PlayerAttackPrimaryEvent e)
    {
        var player = e.Player;
		
        // Создание снаряда
        Bullet bullet = Root.Instance.PackedScenes.World.Bullet.Instantiate() as Bullet;
        // Установка начальной позиции снаряда
        bullet.GlobalPosition = player.GlobalPosition;
        // Установка направления движения снаряда
        bullet.Rotation = player.Rotation;
        bullet.Author = Bullet.AuthorEnum.PLAYER;
        bullet.Speed *= 3;
        bullet.RemainingDamage = player.PrimaryDamage;
        bullet.RemainingDistance = player.PrimaryDistance;
        bullet.Scale *= 2;
        bullet.Source = player;
        Audio2D.PlaySoundAt(Sfx.SmallLaserShot, player.Position, 1f).PitchVariation(0.05f);
        player.GetParent().AddChild(bullet);
    }

    [EventListener]
    public void OnPlayerAttackSecondary(PlayerAttackSecondaryEvent e)
    {
        var player = e.Player;
        if (!Input.IsActionPressed(Keys.AttackSecondary)) return;
		
        Audio2D.PlaySoundAt(Sfx.SmallLaserShot, player.Position, 0.5f);
        var bulletsCount = 5;
        var spread = Mathf.DegToRad(18);
        var speedSpread = 0.1;
		
        for (int i = 0; i < bulletsCount; i++)
        {
            // Создание снаряда
            Bullet bullet = Root.Instance.PackedScenes.World.Bullet.Instantiate() as Bullet;
            // Установка начальной позиции снаряда
            bullet.GlobalPosition = player.GlobalPosition;
            // Установка направления движения снаряда
            bullet.Rotation = player.Rotation + Rand.Range(-spread, spread);
            bullet.Author = Bullet.AuthorEnum.PLAYER;
            bullet.Speed = bullet.Speed * 2 + Rand.Range(-bullet.Speed * speedSpread, bullet.Speed * speedSpread);
            bullet.RemainingDistance = player.SecondaryDistance;
            bullet.RemainingDamage = player.SecondaryDamage;
            bullet.Source = player;
            player.GetParent().AddChild(bullet);
        }
    }
}