using Game.Content;
using Godot;
using KludgeBox.Events;

[GameService]
public class PlayerXpService
{
    
    public double RequiredXpLevelFactor { get; set; } = 1.5;
    public int BasicRequiredXp { get; set; } = 10;
    
    public PlayerXpService()
    {
        EventBus.Subscribe<PlayerGainXpEvent>(OnPlayerGainXpEvent);
    }
    
    public void OnPlayerGainXpEvent(PlayerGainXpEvent playerGainXpEvent) {
        AddXp(playerGainXpEvent.Player, playerGainXpEvent.Xp);
    }
    
    public void AddXp(Player player, int amount)
    {
        player.Xp += amount;
        while (player.Xp >= GetRequiredXp(player))
        {
            LevelUp(player);
        }
    }
	
    public void LevelUp(Player player)
    {
        player.Xp -= GetRequiredXp(player);
        player.Level++;
		
        player.MaxHp *= 1.1;
        player.RegenHpSpeed *= 1.11;
        player.Hp = player.MaxHp;

        player.PrimaryDamage *= 1.05;
        player.SecondaryDamage *= 1.05;

        player.MovementSpeed *= 1.05;
		
        player.AttackSpeed *= 1.1;
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

    [GameEventListener]
    public void GetRequiredXpQuery(PlayerGetRequiredXpQuery playerGetRequiredXpQuery)
    {
        int result = GetRequiredXp(playerGetRequiredXpQuery.Player);
        playerGetRequiredXpQuery.SetResult(result);
        //TODO вместо SetResult хочу, чтобы это было под капотом. А тут просто return result;
        //Ответ на TODO это больно и страшно
    }

    private int GetRequiredXp(Player player)
    {
        return (int) (BasicRequiredXp * Mathf.Pow(RequiredXpLevelFactor, player.Level));
    }
}