using System;
using Godot;
using KludgeBox.Core.Stats;
using KludgeBox.DI.Requests.LoggerInjection;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Controller;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Synchronizer;
using Serilog;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;

public class CharacterStats
{
    public bool IsDead { get; private set; }
    public double Hp { get; private set; }
    public double DutyHp { get; private set; }
    private readonly StatModifiersContainer<CharacterStat> _statModifiersContainer = new();
    
    private readonly Character _character;
    private readonly CharacterSynchronizer _synchronizer;
    [Logger] private ILogger _log;

    public CharacterStats(Character character, CharacterSynchronizer synchronizer)
    {
        Di.Process(this);
        
        _character = character;
        _synchronizer = synchronizer;
    }
    
    /// <param name="damager">Character who deal this damage</param>
    /// <param name="value">Hp will be decreased for this value</param>
    /// <param name="armorIgnore">Armor will be ignored and character will take full damage</param>
    public void Damage(Character damager, double value, bool armorIgnore = false)
    {
        if (value < 0)
        {
            _log.Error(new Exception(), "Damage value must be positive: {damage}", value);
            return;
        }
        if (IsDead) return;

        double absorbByArmor = !armorIgnore && value > Armor ? 0 : value * ArmorAbsorption;
        double hpDamage = value - absorbByArmor;
        Hp -= hpDamage;
        _synchronizer.Stats_OnDamage(damager, hpDamage, absorbByArmor, Hp);
        if (Hp <= 0) Kill(damager);
    }
    
    /// <param name="healer">Character who deal this heal</param>
    /// <param name="value">Hp will be increased for this value</param>
    /// <param name="maxHpMultBonus"><c>MaxHp</c> for this heal will be <c>MaxHp*maxHpMultBonus</c></param>
    public void Heal(Character healer, double value, double maxHpMultBonus = 1)
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
        double maxHpWithBonus = MaxHp * maxHpMultBonus;
        double canDecreaseShortageUnderMaxHp = Math.Min(Math.Max(maxHpWithBonus - Hp, 0), value);
        Hp += canDecreaseShortageUnderMaxHp;
        
        _synchronizer.Stats_OnHeal(healer, canDecreaseDuty + canDecreaseShortageUnderMaxHp, Hp, DutyHp);
    }

    public void Kill(Character killer)
    {
        if (IsDead) return;
        IsDead = true;
        Hp = 0;
        _character.Controller.AddBlock(ControlBlocker.CharacterIsDead);
        _synchronizer.Stats_OnKill(killer);
    }

    public void ResurrectWithPercentHeal(Character resurrector, double healPercent) =>
        ResurrectWithFixedHeal(resurrector, MaxHp*healPercent, healPercent);
    
    public void ResurrectWithFixedHeal(Character resurrector, double healFixed, double maxHpMultBonus = 1)
    {
        if (!IsDead) return;
        IsDead = false;
        Hp = 0;
        _synchronizer.Stats_OnResurrect(resurrector);
        _character.Controller.RemoveBlock(ControlBlocker.CharacterIsDead);
        
        Heal(resurrector, healFixed, maxHpMultBonus);
    }

    #region PhysicsProcess methods
    public void OnPhysicsProcess(double delta)
    {
        RegenProcess(delta);
    }

    private void RegenProcess(double deltaTime)
    {
        double deltaHp = RegenHp * deltaTime;
        // Check and don't heal if nothing for healing. It is network optimization.
        if (deltaHp > 0 && DutyHp + Mathf.Max(MaxHp-Hp, 0) > 0) Heal(_character, deltaHp);
    }
    #endregion

    #region Proxy methods with clamp for all CharacterStat
    public double MaxHp => Mathf.Max(GetRawStat(CharacterStat.MaxHp), 0);
    public double RegenHp => Mathf.Max(GetRawStat(CharacterStat.RegenHp), 0);
    public double Armor => Mathf.Max(GetRawStat(CharacterStat.Armor), 0);
    public double ArmorAbsorption => Mathf.Clamp(GetRawStat(CharacterStat.ArmorAbsorption), 0, 1);
    public double MovementSpeed => Mathf.Max(GetRawStat(CharacterStat.MovementSpeed), 0);
    public double RotationSpeed => Mathf.Max(GetRawStat(CharacterStat.RotationSpeed), 0);
    public double Mass => Mathf.Max(GetRawStat(CharacterStat.Mass), 0.1);
    public double SkillRange(double baseValue) => Mathf.Max(GetRawStat(CharacterStat.SkillRange, baseValue), 0);
    public double SkillDamage(double baseValue) => Mathf.Max(GetRawStat(CharacterStat.SkillDamage, baseValue), 0);
    public double SkillHeal(double baseValue) => Mathf.Max(GetRawStat(CharacterStat.SkillHeal, baseValue), 0);
    public double SkillSpeed(double baseValue) => Mathf.Max(GetRawStat(CharacterStat.SkillSpeed, baseValue), 0);
    public double SkillCooldown(double baseValue) => Mathf.Max(GetRawStat(CharacterStat.SkillCooldown, baseValue), 0);
    public double SkillCritChance => Mathf.Clamp(GetRawStat(CharacterStat.SkillCritChance), 0, 1);
    public double SkillCritModifier => Mathf.Max(GetRawStat(CharacterStat.SkillCritModifier), 0);
    #endregion
    
    #region Proxy methods with sync for StatModifiersContainer
    public void AddStatModifier(StatModifier<CharacterStat> statModifier)
    {
        _statModifiersContainer.AddStatModifier(statModifier);
        SyncStat(statModifier.Stat);
    }

    public bool RemoveStatModifier(StatModifier<CharacterStat> statModifier)
    {
        bool removed = _statModifiersContainer.RemoveStatModifier(statModifier);
        if (removed) SyncStat(statModifier.Stat);
        return removed;
    }

    private void SyncStat(CharacterStat stat)
    {
        double updatedAdditive = GetRawStatValue(stat, StatModifier<CharacterStat>.ModifierType.Additive);
        double updatedMultiplicative = GetRawStatValue(stat, StatModifier<CharacterStat>.ModifierType.Multiplicative);
        _synchronizer.Stats_OnStatUpdate(stat, updatedAdditive, updatedMultiplicative);
    }
    
    public double GetRawStatValue(CharacterStat stat, StatModifier<CharacterStat>.ModifierType type) => _statModifiersContainer.GetStatValue(stat, type);
    public double GetRawStat(CharacterStat stat, double baseValue = 0) => _statModifiersContainer.GetStat(stat, baseValue);
    #endregion
}