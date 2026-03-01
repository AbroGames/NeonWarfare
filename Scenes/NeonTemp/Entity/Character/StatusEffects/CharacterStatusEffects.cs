using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Synchronizer;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffects;

public class CharacterStatusEffects
{
    public IReadOnlyList<StatusEffect> StatusEffects => _statusEffects;
    public IReadOnlyDictionary<string, List<StatusEffect>> StatusEffectsByIdCache => _statusEffectsByIdCache;
    public IReadOnlyDictionary<string, List<StatusEffect>> StatusEffectsByTagCache => _statusEffectsByTagCache;
    
    private readonly List<StatusEffect> _statusEffects = new();
    private readonly Dictionary<string, List<StatusEffect>> _statusEffectsByIdCache = new();
    private readonly Dictionary<string, List<StatusEffect>> _statusEffectsByTagCache = new();
    
    private readonly Dictionary<StatusEffect, int> _clientIdByStatusEffects = new();
    private int _nextClientId = 1;

    private readonly Character _character;
    private readonly CharacterSynchronizer _synchronizer;

    public CharacterStatusEffects(Character character, CharacterSynchronizer synchronizer)
    {
        Di.Process(this);
        
        _character = character;
        _synchronizer = synchronizer;
    }

    public void AddStatusEffect(StatusEffect newStatusEffect, Character author)
    {
        newStatusEffect.AddingPolicy.OnAdd(
            _character,
            author,
            newStatusEffect,
            StatusEffects,
            StatusEffectsByIdCache,
            StatusEffectsByTagCache,
            AddStatusEffectAndSync,
            RemoveStatusEffectAndSync);
    }
    
    public void RemoveStatusEffect(StatusEffect oldStatusEffect)
    {
        RemoveStatusEffectAndSync(oldStatusEffect);
    }

    public void OnPhysicsProcess(double delta)
    {
        List<StatusEffect> forRemove = [];
        foreach (StatusEffect statusEffect in _statusEffects)
        {
            statusEffect.OnPhysicsProcess(delta);
            if (statusEffect.IsFinished) forRemove.Add(statusEffect);
        }

        foreach (StatusEffect statusEffect in forRemove)
        {
            RemoveStatusEffectAndSync(statusEffect);
        }
    }

    private void AddStatusEffectAndSync(StatusEffect newStatusEffect, Character author)
    {
        _statusEffects.Add(newStatusEffect);
        AddToCache(_statusEffectsByIdCache, newStatusEffect.Id, newStatusEffect);
        foreach (string statusEffectTag in newStatusEffect.Tags)
        {
            AddToCache(_statusEffectsByTagCache, statusEffectTag, newStatusEffect);
        }

        SendAddStatusEffectToClient(newStatusEffect);
        newStatusEffect.OnApplied(_character, author);
    }
    
    private void RemoveStatusEffectAndSync(StatusEffect oldStatusEffect)
    {
        if (!_statusEffects.Remove(oldStatusEffect)) return;
        RemoveFromCache(_statusEffectsByIdCache, oldStatusEffect.Id, oldStatusEffect);
        foreach (string statusEffectTag in oldStatusEffect.Tags)
        {
            RemoveFromCache(_statusEffectsByTagCache, statusEffectTag, oldStatusEffect);
        }
        
        SendRemoveStatusEffectToClient(oldStatusEffect);
        oldStatusEffect.OnRemoved(_character);
    }

    private void SendAddStatusEffectToClient(StatusEffect newStatusEffect)
    {
        int clientId = _nextClientId++;
        _clientIdByStatusEffects[newStatusEffect] = clientId;

        ClientStatusEffect newClientStatusEffect = newStatusEffect.GetClientStatusEffect();
        _synchronizer.StatusEffects_OnClientApply(clientId, newClientStatusEffect);
    }
    
    private void SendRemoveStatusEffectToClient(StatusEffect oldStatusEffect)
    {
        int clientId = _clientIdByStatusEffects[oldStatusEffect];
        _clientIdByStatusEffects.Remove(oldStatusEffect);
        
        _synchronizer.StatusEffects_OnClientRemove(clientId);
    }

    private void AddToCache(Dictionary<string, List<StatusEffect>> cache, string key, StatusEffect newStatusEffect)
    {
        if (!cache.ContainsKey(key)) cache[key] = [];
        cache[key].Add(newStatusEffect);
    }
    
    private bool RemoveFromCache(Dictionary<string, List<StatusEffect>> cache, string key, StatusEffect newStatusEffect)
    {
        if (!cache.ContainsKey(key)) return false;
        return cache[key].Remove(newStatusEffect);
    }
}