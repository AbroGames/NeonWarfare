using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MessagePack;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Synchronizer;
using NeonWarfare.Scenes.NeonTemp.Service;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect;

public class CharacterStatusEffectsClient
{
    public ReadOnlyDictionary<int, AbstractClientStatusEffect> StatusEffectByClientId => new(_clientStatusEffectByClientId);
    private readonly Dictionary<int, AbstractClientStatusEffect> _clientStatusEffectByClientId = new();

    private readonly Character _character;
    private readonly CharacterSynchronizer _synchronizer;

    public CharacterStatusEffectsClient(Character character, CharacterSynchronizer synchronizer)
    {
        Di.Process(this);
        
        _character = character;
        _synchronizer = synchronizer;
    }
    
    public void OnAddStatusEffect(int clientId, AbstractClientStatusEffect clientStatusEffect)
    {
        if (_clientStatusEffectByClientId.ContainsKey(clientId))
        {
            throw new ArgumentException($"Client {clientId} has already been added");
        }
        
        _clientStatusEffectByClientId[clientId] = clientStatusEffect;
        clientStatusEffect!.OnClientApplied(_character);
    }
    
    public void OnRemoveStatusEffect(int clientId)
    {
        if (_clientStatusEffectByClientId.Remove(clientId, out var clientStatusEffect))
        {
            clientStatusEffect.OnClientRemoved(_character);
        }
    }
    
    public void OnPhysicsProcess(double delta)
    {
        foreach (AbstractClientStatusEffect statusEffect in _clientStatusEffectByClientId.Values)
        {
            statusEffect.OnClientPhysicsProcess(delta);
        }
    }
}