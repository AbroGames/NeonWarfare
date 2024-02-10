using System.Linq;
using Godot;

public class BattleHudWaveService
{
    
    public double FadeInTime { get; set; } = 0.5;
    public double HoldTime { get; set; } = 1;
    public double FadeOutTime { get; set; } = 0.5;
    
    public BattleHudWaveService()
    {
        Root.Instance.EventBus.Subscribe<BattleHudReadyEvent>(OnBattleHudReadyEvent);
        Root.Instance.EventBus.Subscribe<BattleWorldNewWaveEvent>(OnBattleWorldNewWaveEvent);
    }
    
    public void OnBattleHudReadyEvent(BattleHudReadyEvent battleHudReadyEvent)
    {
        InitBattleHud(battleHudReadyEvent.BattleHud);
    }
    
    public void OnBattleWorldNewWaveEvent(BattleWorldNewWaveEvent battleWorldNewWaveEvent) 
    {
        ShowWaveMessage(battleWorldNewWaveEvent.BattleWorld.BattleHud, battleWorldNewWaveEvent.WaveNumber);
    }

    public void InitBattleHud(BattleHud battleHud)
    {
        battleHud.WaveMessageInitialPosition = battleHud.WaveMessage.Position;
    }
    
    public void ShowWaveMessage(BattleHud battleHud, int waveNumber)
    {
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