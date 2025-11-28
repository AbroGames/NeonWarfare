using System;
using System.Collections.Generic;
using MessagePack;
using NeonWarfare.Scenes.NeonTemp.Service;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect;

public class CharacterClientStatusEffects(Character character)
{
    private readonly Dictionary<int, AbstractClientStatusEffect> _clientStatusEffectByClientId = new();

    public void AddStatusEffect(int clientId, int typeId, byte[] payload)
    {
        if (_clientStatusEffectByClientId.ContainsKey(clientId))
        {
            throw new ArgumentException($"Client {clientId} has already been added");
        }
        
        Type targetType = StatusEffectTypesStorageService.Instance.GetType(typeId);
        var clientStatusEffect = (AbstractClientStatusEffect) MessagePackSerializer.Deserialize(targetType, payload);
        _clientStatusEffectByClientId[clientId] = clientStatusEffect;
        clientStatusEffect.OnClientApplied(character);
    }
    
    public void RemoveStatusEffect(int clientId)
    {
        if (_clientStatusEffectByClientId.Remove(clientId, out var clientStatusEffect))
        {
            clientStatusEffect.OnClientRemoved(character);
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