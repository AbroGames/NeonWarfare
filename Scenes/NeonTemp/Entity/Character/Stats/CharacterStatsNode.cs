using System;
using Godot;
using Godot.Collections;
using KludgeBox.DI.Requests.LoggerInjection;
using KludgeBox.DI.Requests.ParentInjection;
using KludgeBox.Godot.Nodes.MpSync;
using NeonWarfare.Scenes.NeonTemp.Stats;
using Serilog;
using static Godot.SceneReplicationConfig.ReplicationMode;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;

public partial class CharacterStatsNode : Node
{
    
    [Sync(Always)] public bool IsDead { get; private set; }
    [Sync(Always)] public double Hp { get; private set; }
    [Sync] public double DutyHp { get; private set; }
    
    [Sync(Always)] private Dictionary<CharacterStat, double> _statsValuesSynchronizer = new();
    private StatModifiersContainer<CharacterStat> _statModifiersContainer;
    
    [Parent] private Character _character;
    [Logger] private ILogger _log;

    public override void _Ready()
    {
        Di.Process(this);
        Net.DoServer(() => _statModifiersContainer = new());
    }
    
    /// <param name="value">Hp will be decrease for this value</param>
    public void Damage(double value)
    {
        if (value < 0) _log.Error(new Exception(), "Damage value must be positive: {damage}", value);
        if (IsDead) return;
        
        //TODO rpc-event fot CharacterClientStatsNode ? And heal/kill/res
        Hp -= value;
        if (Hp <= 0) Kill();
    }
    
    /// <param name="value">Hp will be increase for this value</param>
    /// <param name="maxHpMultBonus"><c>MaxHp</c> for this heal will be <c>MaxHp*(1+maxHpMultBonus)</c></param>
    public void Heal(double value, double maxHpMultBonus = 0)
    {
        if (value < 0) _log.Error(new Exception(), "Heal value must be positive: {heal}", value);
        if (maxHpMultBonus < 0) _log.Error(new Exception(), "Heal must has positive MaxHpMultBonus: {maxHpMultBonus}", maxHpMultBonus);
        if (IsDead) return;
        
        double canDecreaseDuty = Math.Min(DutyHp, value);
        DutyHp -= canDecreaseDuty;
        value -= canDecreaseDuty;
        
        // Use Max(delta, 0) for case when Hp more than MaxHp (because of previous maxHpWithBonus heals) 
        double maxHpWithBonus = MaxHp * (1 + maxHpMultBonus);
        double canDecreaseShortageUnderMaxHp = Math.Min(Math.Max(maxHpWithBonus - Hp, 0), value);
        Hp += canDecreaseShortageUnderMaxHp;
        value -= canDecreaseShortageUnderMaxHp;
    }

    public void Kill()
    {
        if (IsDead) return;
        IsDead = true;
    }

    public void Resurrect()
    {
        if (!IsDead) return;
        IsDead = false;
    }
    
    public double GetStat(CharacterStat stat)
    {
        return Net.IsServer() ? 
            _statModifiersContainer.GetStat(stat) :
            _statsValuesSynchronizer[stat];
    }

    #region _PhysicsProcess methods
    public override void _PhysicsProcess(double delta)
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
            _statsValuesSynchronizer[stat] = GetStat(stat);
        }
    }
    #endregion

    #region Proxy methods for all CharacterStat
    public double MaxHp => GetStat(CharacterStat.MaxHp);
    public double RegenHp => GetStat(CharacterStat.RegenHp);
    public double DrainHp => GetStat(CharacterStat.DrainHp);
    public double Armor => GetStat(CharacterStat.Armor);
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
    public double GetStatValue(CharacterStat stat, StatModifier<CharacterStat>.ModifierType type) => _statModifiersContainer.GetStatValue(stat, type);
    #endregion
}