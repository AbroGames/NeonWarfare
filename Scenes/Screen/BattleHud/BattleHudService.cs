using System.Linq;

public class BattleHudService
{

    private PlayerXpService _playerXpService;
    
    public BattleHudService(PlayerXpService playerXpService)
    {
        _playerXpService = playerXpService;
        
        Root.Instance.EventBus.Subscribe<BattleHudReadyEvent>(OnBattleHudReadyEvent);
        Root.Instance.EventBus.Subscribe<BattleHudProcessEvent>(OnBattleHudProcessEvent);
        Root.Instance.EventBus.Subscribe<BattleWorldNewWaveEvent>(OnBattleWorldNewWaveEvent);
    }
    
    public void OnBattleHudReadyEvent(BattleHudReadyEvent battleHudReadyEvent)
    {
        InitBattleHud(battleHudReadyEvent.BattleHud);
    }
    
    public void OnBattleHudProcessEvent(BattleHudProcessEvent battleHudProcessEvent) 
    {
        UpdateBattleHud(battleHudProcessEvent.BattleHud, battleHudProcessEvent.BattleHud.BattleWorld, battleHudProcessEvent.Delta);
    }
    
    public void OnBattleWorldNewWaveEvent(BattleWorldNewWaveEvent battleWorldNewWaveEvent) 
    {
        ShowWaveMessage(battleWorldNewWaveEvent.BattleWorld.BattleHud, battleWorldNewWaveEvent.WaveNumber);
    }

    public void InitBattleHud(BattleHud battleHud)
    {
        battleHud.WaveMessageInitialPosition = battleHud.WaveMessage.Position;
    }

    public void UpdateBattleHud(BattleHud battleHud, BattleWorld battleWorld, double delta)
    {
        int playerRequiredXp = _playerXpService.GetRequiredXp(battleWorld.Player);
        
        battleHud.Xp.Value = (double) battleWorld.Player.Xp / playerRequiredXp;
        battleHud.XpLabel.Text = $"Xp: {battleWorld.Player.Xp} / {playerRequiredXp}";
        battleHud.Level.Text = $"Level: {battleWorld.Player.Level}";
		
        battleHud.Waves.Text = $"Wave: {battleWorld.WaveNumber}";
        battleHud.Enemies.Text = $"Enemies: {battleWorld.Enemies.Count}";

        battleHud.HpBar.CurrentUpperValue = battleWorld.Player.Hp;
        battleHud.HpBar.CurrentLowerValue = battleHud.HpBar.CurrentUpperValue * 1.1;
        battleHud.HpBar.MaxValue = battleWorld.Player.MaxHp;
        battleHud.HpBar.Label.Text = $"Health: {battleWorld.Player.Hp:N0} / {battleWorld.Player.MaxHp:N0}";
		
        battleHud.Deltas.Enqueue(delta);
        if (battleHud.Deltas.Count >= 240)
        {
            battleHud.Deltas.Dequeue();
            var fps = 1 / battleHud.Deltas.Average();
            battleHud.Fps.Text = $"FPS: {fps:N0}";
        }
    }
    
    public void ShowWaveMessage(BattleHud battleHud, int waveNumber)
    {
        battleHud.WaveMessage.Text = $"WAVE {waveNumber}";
        var colorTween = battleHud.GetTree().CreateTween();
        var positionTween = battleHud.GetTree().CreateTween();

        double fadeInTime = 0.5;
        double holdTime = 1;
        double fadeOutTime = 0.5;

        colorTween.TweenProperty(battleHud.WaveMessage, "modulate:a", 1f, fadeInTime);
        colorTween.TweenInterval(holdTime);
        colorTween.TweenProperty(battleHud.WaveMessage, "modulate:a", 0f, fadeOutTime);
		
        positionTween.TweenProperty(battleHud.WaveMessage, "position", battleHud.WaveMessageInitialPosition + Vec(0,50), fadeInTime);
        positionTween.TweenInterval(holdTime);
        positionTween.TweenProperty(battleHud.WaveMessage, "position", battleHud.WaveMessageInitialPosition, fadeOutTime);
    }
}