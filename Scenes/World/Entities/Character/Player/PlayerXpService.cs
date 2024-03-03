using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;

namespace NeoVector;

[GameService]
public class PlayerXpService
{

    [EventListener]
    public void OnPlayerReadyEvent(PlayerReadyEvent playerReadyEvent)
    {
        Player player = playerReadyEvent.Player;
        player.NextLevelXp = EventBus.Require(new PlayerGetRequiredXpQuery(player));
    }
    
    [EventListener]
    public void OnPlayerGainXpEvent(PlayerGainXpEvent playerGainXpEvent)
    {
        var (player, gainXp) = playerGainXpEvent;
        
        player.Xp += gainXp;
        while (player.Xp >= player.NextLevelXp)
        {
            EventBus.Publish(new PlayerLevelUpEvent(player));
        }
    }
    
    [EventListener]
    public long GetRequiredXpQuery(PlayerGetRequiredXpQuery playerGetRequiredXpQuery)
    {
        Player player = playerGetRequiredXpQuery.Player;
        
        return (long) (player.BasicRequiredXp * Mathf.Pow(player.RequiredXpLevelFactor, player.Level));
    }
	
    [EventListener]
    public void OnPlayerLevelUpEvent(PlayerLevelUpEvent playerLevelUpEvent)
    {
        Player player = playerLevelUpEvent.Player;
        var amount = playerLevelUpEvent.Amount;

        for (int i = 0; i < amount; i++)
        {
            player.Xp -= player.NextLevelXp;
            player.Level++;
            player.NextLevelXp = EventBus.Require(new PlayerGetRequiredXpQuery(player));
		
            player.MaxHp *= 1.1;
            player.RegenHpSpeed *= 1.11;
            player.Hp = player.MaxHp;

            player.PrimaryDamage *= 1.05;
            player.SecondaryDamage *= 1.05;

            player.MovementSpeed *= 1.05;
		
            player.PrimaryCd.Duration /= 1.1;
            player.SecondaryCd.Duration /= 1.05;

            player.RotationSpeed *= 1.05;

            player.PrimaryDistance *= 1.05;
            player.SecondaryDistance *= 1.05;

            player.UniversalDamageMultiplier *= 1.05;
        
            //var zoomTween = player.GetTree().CreateTween();
            //zoomTween.SetTrans(Tween.TransitionType.Cubic);
            //zoomTween.TweenProperty(player.Camera, "zoom", player.Camera.Zoom / 1.05, 1);
        
            Audio2D.PlaySoundOn(Sfx.LevelUp, player, 1f).PitchVariation(0.05f);
            var lvlUpLabel = Root.Instance.PackedScenes.World.FloatingLabel.Instantiate<FloatingLabel>();
		
            lvlUpLabel.Configure($"Level up!\n{player.Level-1} -> {player.Level}", Colors.Gold, 1.3);
            lvlUpLabel.Position = player.Position - Vec(0, 100);
            player.GetParent().AddChild(lvlUpLabel);
        }
    }
}