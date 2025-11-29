using NeonWarfare.Scenes.NeonTemp.Entity.Character.Synchronizer;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;

public class CharacterStatsClient
{    
    private readonly Character _character;
    private readonly CharacterSynchronizer _synchronizer;

    public CharacterStatsClient(Character character, CharacterSynchronizer synchronizer)
    {
        Di.Process(this);
        
        _character = character;
        _synchronizer = synchronizer;
    }

    public void OnDamage(double value, double newHp)
    {
        
    }
    
    public void OnHeal(double value, double newHp, double newDutyHp)
    {
        
    }
    
    public void OnKill()
    {
        
    }

    public void OnResurrect()
    {
        
    }
    
    public void OnPhysicsProcess(double delta) { }
    
    #region Proxy methods for CharacterSynchronizer
    public bool IsDead => _synchronizer.StatsIsDead;
    public double Hp => _synchronizer.StatsHp;
    public double DutyHp => _synchronizer.StatsDutyHp;
    public double GetStat(CharacterStat stat) => _synchronizer.Stats_GetStat(stat);
    #endregion
}