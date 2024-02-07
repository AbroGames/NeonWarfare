using System;
using Game.Content;
using Godot;

public class PlayerXpService
{
    public PlayerXpService()
    {
        Root.Instance.EventBus.Subscribe<PlayerGainXpEvent>(OnPlayerGainXpEvent);
    }
    
    public void OnPlayerGainXpEvent(PlayerGainXpEvent playerGainXpEvent) {
        AddXp(playerGainXpEvent.Player, playerGainXpEvent.Xp);
    }
    
    private void AddXp(Player player, int amount)
    {
        player.Xp += amount;
        while (player.Xp >= player.RequiredXp)
        {
            LevelUp(player);
        }
    }
	
    private void LevelUp(Player player)
    {
        player.Xp -= player.RequiredXp;
        player.Level++;
		
        player.MaxHp *= 1.1;
        player.RegenHpSpeed *= 1.1;
        player.Hp = player.MaxHp;

        player.PrimaryDamage *= 1.1;
        player.SecondaryDamage *= 1.1;

        player.MovementSpeed *= 1.05;
		
        player.AttackSpeed *= 1.1;
        player._secondaryCd.Duration /= 1.1;

        player.RotationSpeed *= 1.1;

        player.PrimaryDistance *= 1.1;
        player.SecondaryDistance *= 1.1;


        var zoomTween = player.GetTree().CreateTween();
        zoomTween.SetTrans(Tween.TransitionType.Cubic);
        zoomTween.TweenProperty(player.Camera, "zoom", player.Camera.Zoom / 1.05, 1);
		
		
        Audio2D.PlaySoundOn(Sfx.LevelUp, player, 1f);
        var lvlUpLabel = Root.Instance.PackedScenes.World.FloatingLabel.Instantiate<FloatingLabel>();
		
        lvlUpLabel.Configure($"Level up!\n({player.Level-1} -> {player.Level})", Colors.Gold, 1.3);
        lvlUpLabel.Position = player.Position - Vec(0, 100);
        player.GetParent().AddChild(lvlUpLabel);
    }
}