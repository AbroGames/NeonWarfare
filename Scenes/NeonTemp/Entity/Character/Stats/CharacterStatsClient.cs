using System.Collections.Generic;
using Godot;
using KludgeBox.Core.Stats;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Synchronizer;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;

public class CharacterStatsClient
{    
    
    public bool IsDead;
    public double Hp;
    public double DutyHp;
    
    private record StatPair(StatModifier<CharacterStat> Additive, StatModifier<CharacterStat> Multiplicative);
    private readonly StatModifiersContainer<CharacterStat> _statModifiersContainer = new();
    private readonly Dictionary<CharacterStat, StatPair> _addedStats = new();
    
    private readonly Character _character;
    private readonly CharacterSynchronizer _synchronizer;

    public CharacterStatsClient(Character character, CharacterSynchronizer synchronizer)
    {
        Di.Process(this);
        
        _character = character;
        _synchronizer = synchronizer;
    }

    public void OnDamage(Character damager, double value, double absorbByArmor, double newHp)
    {
        
    }
    
    public void OnHeal(Character healer, double value, double newHp, double newDutyHp)
    {
        
    }
    
    public void OnKill(Character killer)
    {
        
    }

    public void OnResurrect(Character resurrector)
    {
        
    }

    public void OnStatUpdate(CharacterStat stat, double additive, double multiplicative)
    {
        if (_addedStats.TryGetValue(stat, out var addedStat))
        {
            _statModifiersContainer.RemoveStatModifier(addedStat.Additive);
            _statModifiersContainer.RemoveStatModifier(addedStat.Multiplicative);
        }

        StatPair statPair = new StatPair(
            StatModifier<CharacterStat>.CreateAdditive(stat, additive),
            StatModifier<CharacterStat>.CreateMultiplicative(stat, multiplicative));
        _addedStats[stat] = statPair;
        
        _statModifiersContainer.AddStatModifier(statPair.Additive);
        _statModifiersContainer.AddStatModifier(statPair.Multiplicative);
    }
    
    public void OnPhysicsProcess(double delta) { }
    
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
    
    #region Proxy methods for CharacterSynchronizer
    public double GetRawStatValue(CharacterStat stat, StatModifier<CharacterStat>.ModifierType type) => _statModifiersContainer.GetStatValue(stat, type);
    public double GetRawStat(CharacterStat stat, double baseValue = 0) => _statModifiersContainer.GetStat(stat, baseValue);
    #endregion
    
}