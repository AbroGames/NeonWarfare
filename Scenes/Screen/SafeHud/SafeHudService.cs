using AbroDraft.Scenes.World.Entities.Character.Player;
using AbroDraft.Scenes.World.SafeWorld;
using AbroDraft.Scripts.EventBus;
using Godot;
using KludgeBox;
using KludgeBox.Events;

namespace AbroDraft.Scenes.Screen.SafeHud;

[GameService]
public class SafeHudService
{
    
    [GameEventListener]
    public void OnSafeHudProcessEvent(SafeHudProcessEvent safeHudProcessEvent)
    {
        SafeHud safeHud = safeHudProcessEvent.SafeHud;
        SafeWorld safeWorld = safeHud.SafeWorld;
        Player player = safeWorld.Player;
        
        int playerRequiredXp = EventBus.Require(new PlayerGetRequiredXpQuery(player));
        
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