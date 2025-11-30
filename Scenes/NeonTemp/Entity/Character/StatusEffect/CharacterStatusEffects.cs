using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MessagePack;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Synchronizer;
using NeonWarfare.Scenes.NeonTemp.Service;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect;

public class CharacterStatusEffects
{
    public ReadOnlyDictionary<string, List<AbstractStatusEffect>> StatusEffectsById => new(_statusEffectsById);
    private readonly Dictionary<string, List<AbstractStatusEffect>> _statusEffectsById = new();
    private readonly Dictionary<AbstractStatusEffect, int> _clientIdByStatusEffects = new();
    private int _nextClientId = 1;

    private readonly Character _character;
    private readonly CharacterSynchronizer _synchronizer;

    public CharacterStatusEffects(Character character, CharacterSynchronizer synchronizer)
    {
        Di.Process(this);
        
        _character = character;
        _synchronizer = synchronizer;
    }

    public void AddStatusEffect(AbstractStatusEffect newStatusEffect, Character author)
    {
        newStatusEffect.AddingPolicy.OnAdd(
            _character,
            author,
            newStatusEffect,
            GetReadOnlyStatusEffects,
            _statusEffectsById.GetValueOrDefault(newStatusEffect.Id, []),
            AddStatusEffectAndSync,
            RemoveStatusEffectAndSync);
    }
    
    public void RemoveStatusEffect(AbstractStatusEffect oldStatusEffect)
    {
        RemoveStatusEffectAndSync(oldStatusEffect);
    }

    public void OnPhysicsProcess(double delta)
    {
        List<AbstractStatusEffect> forRemove = [];
        foreach (List<AbstractStatusEffect> statusEffects in _statusEffectsById.Values)
        {
            foreach (AbstractStatusEffect statusEffect in statusEffects)
            {
                statusEffect.OnPhysicsProcess(delta);
                if (statusEffect.IsFinished) forRemove.Add(statusEffect);
            }
        }

        foreach (AbstractStatusEffect statusEffect in forRemove)
        {
            RemoveStatusEffectAndSync(statusEffect);
        }
    }

    private void AddStatusEffectAndSync(AbstractStatusEffect newStatusEffect, Character author)
    {
        if (!_statusEffectsById.ContainsKey(newStatusEffect.Id))
        {
            _statusEffectsById[newStatusEffect.Id] = [];
        }
        _statusEffectsById[newStatusEffect.Id].Add(newStatusEffect);

        SendAddStatusEffectToClient(newStatusEffect);
        newStatusEffect.OnApplied(_character, author);
    }
    
    private void RemoveStatusEffectAndSync(AbstractStatusEffect oldStatusEffect)
    {
        if (!_statusEffectsById.ContainsKey(oldStatusEffect.Id))
        {
            return;
        }
        if (!_statusEffectsById[oldStatusEffect.Id].Remove(oldStatusEffect)) return;
        
        
        SendRemoveStatusEffectToClient(oldStatusEffect);
        oldStatusEffect.OnRemoved(_character);
    }

    private Dictionary<string, IReadOnlyCollection<AbstractStatusEffect>> GetReadOnlyStatusEffects()
    {
        return _statusEffectsById.ToDictionary(
            kv => kv.Key,
            kv => (IReadOnlyCollection<AbstractStatusEffect>) kv.Value);
    }

    private void SendAddStatusEffectToClient(AbstractStatusEffect newStatusEffect)
    {
        int clientId = _nextClientId++;
        _clientIdByStatusEffects[newStatusEffect] = clientId;

        AbstractClientStatusEffect newClientStatusEffect = newStatusEffect.GetClientStatusEffect();
        _synchronizer.StatusEffects_OnClientApply(clientId, newClientStatusEffect);
    }
    
    private void SendRemoveStatusEffectToClient(AbstractStatusEffect oldStatusEffect)
    {
        int clientId = _clientIdByStatusEffects[oldStatusEffect];
        _clientIdByStatusEffects.Remove(oldStatusEffect);
        
        _synchronizer.StatusEffects_OnClientRemove(clientId);
    }
}