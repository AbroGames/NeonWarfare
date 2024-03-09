using Godot;
using KludgeBox;
using KludgeBox.Events;

namespace NeoVector;

[GameService]
public class SafeHudService
{
    
    [EventListener]
    public void OnSafeHudProcessEvent(SafeHudProcessEvent safeHudProcessEvent)
    {
        SafeHud safeHud = safeHudProcessEvent.SafeHud;
        SafeWorld safeWorld = safeHud.SafeWorld;
        Player player = safeWorld.Player;
        

        safeHud.HpBar.CurrentUpperValue = player.Hp;
        safeHud.HpBar.CurrentLowerValue = player.Hp; //TODO сделать аналогично с BattleHud, вынести в общий сервис (не дублировать код)
        safeHud.HpBar.MaxValue = player.MaxHp;
        safeHud.HpBar.Label.Text = $"Health: {player.Hp:N0} / {player.MaxHp:N0}";
        safeHud.Fps.Text = $"FPS: {Engine.GetFramesPerSecond():N0}";
    }
}