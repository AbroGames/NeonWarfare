using System.Linq;
using Godot;

public class SafeHudService
{

    private readonly PlayerXpService _playerXpService;
    
    public SafeHudService(PlayerXpService playerXpService)
    {
        _playerXpService = playerXpService;
        
        Root.Instance.EventBus.Subscribe<SafeHudProcessEvent>(OnSafeHudProcessEvent);
    }
    
    public void OnSafeHudProcessEvent(SafeHudProcessEvent safeHudProcessEvent) 
    {
        UpdateSafeHud(safeHudProcessEvent.SafeHud, safeHudProcessEvent.SafeHud.SafeWorld);
    }

    public void UpdateSafeHud(SafeHud safeHud, SafeWorld safeWorld)
    {
        Player player = safeWorld.Player;
        int playerRequiredXp = _playerXpService.GetRequiredXp(player);
        
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