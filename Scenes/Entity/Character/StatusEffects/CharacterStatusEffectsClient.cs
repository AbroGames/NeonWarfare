using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NeonWarfare.Scenes.Entity.Character.Synchronizer;

namespace NeonWarfare.Scenes.Entity.Character.StatusEffects;

public class CharacterStatusEffectsClient
{
    public ReadOnlyDictionary<int, ClientStatusEffect> StatusEffectByClientId => new(_clientStatusEffectByClientId);
    private readonly Dictionary<int, ClientStatusEffect> _clientStatusEffectByClientId = new();

    private readonly Character _character;
    private readonly CharacterSynchronizer _synchronizer;

    public CharacterStatusEffectsClient(Character character, CharacterSynchronizer synchronizer)
    {
        Di.Process(this);
        
        _character = character;
        _synchronizer = synchronizer;
    }
    
    public void OnAddStatusEffect(int clientId, ClientStatusEffect clientStatusEffect)
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
        foreach (ClientStatusEffect statusEffect in _clientStatusEffectByClientId.Values)
        {
            statusEffect.OnClientPhysicsProcess(delta);
        }
    }
}