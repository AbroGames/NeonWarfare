using System;
using System.Collections.Generic;
using System.Linq;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;

public class StatModifiersContainer
{
    private readonly HashSet<StatModifier> _statsModifiers = new();
    
    private readonly Dictionary<Stat, double> _cacheAdditive = new();
    private readonly Dictionary<Stat, double> _cacheMultiplicative = new();
    private readonly HashSet<(Stat, StatModifier.ModifierType)> _needToInvalidateCache = new();
    
    public void AddStatModifier(StatModifier statModifier)
    {
        AddTaskToInvalidateCache(statModifier);
        statModifier.PropertyChanged += (stat, _) => AddTaskToInvalidateCache((StatModifier) stat);
        
        _statsModifiers.Add(statModifier);
    }
    
    public bool RemoveStatModifier(StatModifier statModifier)
    {
        AddTaskToInvalidateCache(statModifier);
        return _statsModifiers.Remove(statModifier);
    }

    public double CalculateStat(Stat stat, double baseValue)
    {
        double additiveValue = GetValue(stat, StatModifier.ModifierType.Additive);
        double multiplicativeValue = GetValue(stat, StatModifier.ModifierType.Multiplicative);
        return (baseValue + additiveValue) * multiplicativeValue;
    }

    public double GetValue(Stat stat, StatModifier.ModifierType type)
    {
        if (_needToInvalidateCache.Contains((stat, type))) RecalculateCache(stat, type);
        return GetCacheInfo(type).Dictionary.GetValueOrDefault(stat, GetCacheInfo(type).DefaultValue);
    }
    
    private void AddTaskToInvalidateCache(StatModifier statModifier)
    {
        _needToInvalidateCache.Add((statModifier.Stat, statModifier.Type));
    }

    private void RecalculateCache(Stat stat, StatModifier.ModifierType type)
    {
        GetCacheInfo(type).Dictionary[stat] = _statsModifiers
            .Where(sm => sm.Stat == stat)
            .Where(sm => sm.Type == type)
            .Select(sm => sm.Value)
            .Aggregate(GetCacheInfo(type).DefaultValue, GetCacheInfo(type).Operation);
        
        _needToInvalidateCache.Remove((stat, type));
    }
    
    private record CacheInfo( 
        Dictionary<Stat, double> Dictionary, 
        Func<double, double, double> Operation, 
        double DefaultValue);
    
    private CacheInfo GetCacheInfo(StatModifier.ModifierType type)
    {
        return type switch
        {
            StatModifier.ModifierType.Additive => 
                new CacheInfo(_cacheAdditive, (d1, d2) => d1 + d2, 0),
            StatModifier.ModifierType.Multiplicative => 
                new CacheInfo(_cacheMultiplicative, (d1, d2) => d1 * d2, 1),
            _ => throw new ArgumentException("Unknown ModifierType: " + type)
        };
    }
    
}