using System;
using KludgeBox.Core.Stats;
using KludgeBox.DI.Requests.LoggerInjection;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Synchronizer;
using Serilog;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;

public class CharacterStats
{
    public bool IsDead { get; private set; }
    public double Hp { get; private set; }
    public double DutyHp { get; private set; }
    private StatModifiersContainer<CharacterStat> _statModifiersContainer;
    
    private readonly Character _character;
    private readonly CharacterSynchronizer _synchronizer;
    [Logger] private ILogger _log;

    public CharacterStats(Character character, CharacterSynchronizer synchronizer)
    {
        Di.Process(this);
        
        _character = character;
        _synchronizer = synchronizer;
    }
    
    /// <param name="value">Hp will be decreased for this value</param>
    public void Damage(double value)
    {
        if (value < 0)
        {
            _log.Error(new Exception(), "Damage value must be positive: {damage}", value);
            return;
        }
        if (IsDead) return;
        
        Hp -= value;
        _synchronizer.Stats_OnDamage(value, Hp);
        if (Hp <= 0) Kill();
    }
    
    /// <param name="value">Hp will be increased for this value</param>
    /// <param name="maxHpMultBonus"><c>MaxHp</c> for this heal will be <c>MaxHp*(1+maxHpMultBonus)</c></param>
    public void Heal(double value, double maxHpMultBonus = 0)
    {
        if (value < 0)
        {
            _log.Error(new Exception(), "Heal value must be positive: {heal}", value);
            return;
        }
        if (maxHpMultBonus < 0)
        {
            _log.Error(new Exception(), "Heal must has positive MaxHpMultBonus: {maxHpMultBonus}", maxHpMultBonus);
            return;
        }
        if (IsDead) return;
        
        double canDecreaseDuty = Math.Min(DutyHp, value);
        DutyHp -= canDecreaseDuty;
        value -= canDecreaseDuty;
        
        // Use Max(delta, 0) for case when Hp more than MaxHp (because of previous maxHpWithBonus heals) 
        double maxHpWithBonus = MaxHp * (1 + maxHpMultBonus);
        double canDecreaseShortageUnderMaxHp = Math.Min(Math.Max(maxHpWithBonus - Hp, 0), value);
        Hp += canDecreaseShortageUnderMaxHp;
        
        _synchronizer.Stats_OnHeal(canDecreaseDuty + canDecreaseShortageUnderMaxHp, Hp, DutyHp);
    }

    public void Kill()
    {
        if (IsDead) return;
        IsDead = true;
        _synchronizer.Stats_OnKill();
    }

    public void Resurrect()
    {
        if (!IsDead) return;
        IsDead = false;
        _synchronizer.Stats_OnResurrect();
    }

    #region PhysicsProcess methods
    public void OnPhysicsProcess(double delta)
    {
        RegenDrainProcess(delta);
        UpdateStatsValues();
    }

    private void RegenDrainProcess(double deltaTime)
    {
        double deltaHp = (RegenHp - DrainHp) * deltaTime;
        if (deltaHp < 0) Damage(deltaHp);
        if (deltaHp > 0) Heal(deltaHp);
    }
    
    private void UpdateStatsValues()
    {
        foreach (CharacterStat stat in Enum.GetValues<CharacterStat>())
        {
            _synchronizer.Stats_Update(stat, GetStat(stat));
        }
    }
    #endregion

    #region Proxy methods for all CharacterStat
    public double MaxHp => GetStat(CharacterStat.MaxHp);
    public double RegenHp => GetStat(CharacterStat.RegenHp);
    public double DrainHp => GetStat(CharacterStat.DrainHp);
    public double Armor => GetStat(CharacterStat.Armor);
    public double ArmorAbsorption => GetStat(CharacterStat.ArmorAbsorption);
    public double MovementSpeed => GetStat(CharacterStat.MovementSpeed);
    public double RotationSpeed => GetStat(CharacterStat.RotationSpeed);
    public double Mass => GetStat(CharacterStat.Mass);
    public double SkillRange => GetStat(CharacterStat.SkillRange);
    public double SkillDamage => GetStat(CharacterStat.SkillDamage);
    public double SkillHeal => GetStat(CharacterStat.SkillHeal);
    public double SkillSpeed => GetStat(CharacterStat.SkillSpeed);
    public double SkillCooldown => GetStat(CharacterStat.SkillCooldown);
    public double SkillCritChance => GetStat(CharacterStat.SkillCritChance);
    public double SkillCritModifier => GetStat(CharacterStat.SkillCritModifier);
    #endregion
    
    #region Proxy methods for StatModifiersContainer
    public void AddStatModifier(StatModifier<CharacterStat> statModifier) => _statModifiersContainer.AddStatModifier(statModifier);
    public bool RemoveStatModifier(StatModifier<CharacterStat> statModifier) => _statModifiersContainer.RemoveStatModifier(statModifier);
    public double GetStat(CharacterStat stat, double baseValue = 0) => _statModifiersContainer.GetStat(stat, baseValue);
    public double GetStatValue(CharacterStat stat, StatModifier<CharacterStat>.ModifierType type) => _statModifiersContainer.GetStatValue(stat, type);
    #endregion
}