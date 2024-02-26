using Godot;
using KludgeBox;
using KludgeBox.Events;
using NeoVector.World;

namespace NeoVector;

[GameService]
public class BattleHudWaveService
{
    
    public double FadeInTime { get; set; } = 0.5;
    public double HoldTime { get; set; } = 1;
    public double FadeOutTime { get; set; } = 0.5;
    
    [GameEventListener]
    public void OnBattleHudReadyEvent(BattleHudReadyEvent battleHudReadyEvent)
    {
        BattleHud battleHud = battleHudReadyEvent.BattleHud;
        
        battleHud.WaveMessageInitialPosition = battleHud.WaveMessage.Position;
    }
    
    [GameEventListener]
    public void OnBattleWorldNewWaveEvent(BattleWorldNewWaveGeneratedEvent battleWorldNewWaveGeneratedEvent)
    {
        var (battleWorld, waveNumber) = battleWorldNewWaveGeneratedEvent;
        BattleHud battleHud = battleWorld.BattleHud;
        
        battleHud.WaveMessage.Text = $"WAVE {waveNumber}";
        Tween colorTween = battleHud.GetTree().CreateTween();
        Tween positionTween = battleHud.GetTree().CreateTween();

        colorTween.TweenProperty(battleHud.WaveMessage, "modulate:a", 1f, FadeInTime);
        colorTween.TweenInterval(HoldTime);
        colorTween.TweenProperty(battleHud.WaveMessage, "modulate:a", 0f, FadeOutTime);
		
        positionTween.TweenProperty(battleHud.WaveMessage, "position:y", battleHud.WaveMessageInitialPosition.Y + 50, FadeInTime);
        positionTween.TweenInterval(HoldTime);
        positionTween.TweenProperty(battleHud.WaveMessage, "position:y", battleHud.WaveMessageInitialPosition.Y, FadeOutTime);
    }
}