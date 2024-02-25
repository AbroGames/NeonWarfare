using System.Linq;
using Godot;


[GameService]
public class SafeHudService
{
    
    public SafeHudService()
    {
        EventBus.Subscribe<SafeHudProcessEvent>(OnSafeHudProcessEvent);
    }
    
    public void OnSafeHudProcessEvent(SafeHudProcessEvent safeHudProcessEvent) 
    {
        UpdateSafeHud(safeHudProcessEvent.SafeHud, safeHudProcessEvent.SafeHud.SafeWorld);
    }

    public void UpdateSafeHud(SafeHud safeHud, SafeWorld safeWorld)
    {
        Player player = safeWorld.Player;
        
        PlayerGetRequiredXpQuery playerGetRequiredXpQuery = new PlayerGetRequiredXpQuery(player);
        EventBus.Publish(playerGetRequiredXpQuery);
        int playerRequiredXp = playerGetRequiredXpQuery.Result;
        
        safeHud.Xp.Value = (double) player.Xp / playerRequiredXp;
        safeHud.XpLabel.Text = $"Xp: {player.Xp} / {playerRequiredXp}";
        safeHud.Level.Text = $"Level: {player.Level}";

        safeHud.HpBar.CurrentUpperValue = player.Hp;
        safeHud.HpBar.CurrentLowerValue = player.Hp + player.HpCanBeFastRegen;
        safeHud.HpBar.MaxValue = player.MaxHp;
        safeHud.HpBar.Label.Text = $"Health: {player.Hp:N0} / {player.MaxHp:N0}";
        safeHud.Fps.Text = $"FPS: {Engine.GetFramesPerSecond():N0}";
    }
}