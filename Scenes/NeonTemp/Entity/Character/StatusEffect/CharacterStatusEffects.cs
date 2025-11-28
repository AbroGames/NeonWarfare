using System.Collections.Generic;
using System.Linq;
using MessagePack;
using NeonWarfare.Scenes.NeonTemp.Service;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect;

//TODO Синхронизация по сети? Через Rpc, сделать класс нодой? Но слишком много дочерних нод будет. Rpc в родительском partial Character
public class CharacterStatusEffects(Character character) 
{
    private Dictionary<string, List<AbstractStatusEffect>> _statusEffectsById = new();
    private Dictionary<AbstractStatusEffect, int> _clientIdByStatusEffects = new();
    private int _nextClientId = 1;

    public void AddStatusEffect(AbstractStatusEffect newStatusEffect)
    {
        newStatusEffect.AddingPolicy.OnAdd(
            character,
            newStatusEffect,
            GetReadOnlyStatusEffects,
            _statusEffectsById.GetValueOrDefault(newStatusEffect.Id, []),
            AddStatusEffectOnServerAndClient,
            RemoveStatusEffectOnServerAndClient);
    }
    
    public void RemoveStatusEffect(AbstractStatusEffect oldStatusEffect)
    {
        RemoveStatusEffectOnServerAndClient(oldStatusEffect);
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
            RemoveStatusEffectOnServerAndClient(statusEffect);
        }
    }

    private void AddStatusEffectOnServerAndClient(AbstractStatusEffect newStatusEffect)
    {
        if (!_statusEffectsById.ContainsKey(newStatusEffect.Id))
        {
            _statusEffectsById[newStatusEffect.Id] = [];
        }
        _statusEffectsById[newStatusEffect.Id].Add(newStatusEffect);

        SendAddStatusEffectToClient(newStatusEffect);
        newStatusEffect.OnApplied(character);
    }
    
    private void RemoveStatusEffectOnServerAndClient(AbstractStatusEffect oldStatusEffect)
    {
        if (!_statusEffectsById.ContainsKey(oldStatusEffect.Id))
        {
            return;
        }
        if (!_statusEffectsById[oldStatusEffect.Id].Remove(oldStatusEffect)) return;
        
        
        SendRemoveStatusEffectToClient(oldStatusEffect);
        oldStatusEffect.OnRemoved(character);
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
        int typeId = StatusEffectTypesStorageService.Instance.GetId(newClientStatusEffect.GetType());
        byte[] payload = MessagePackSerializer.Serialize(newClientStatusEffect.GetType(), newClientStatusEffect);
        
        character.StatusEffect_OnClientApply(clientId, typeId, payload);
    }
    
    private void SendRemoveStatusEffectToClient(AbstractStatusEffect oldStatusEffect)
    {
        int clientId = _clientIdByStatusEffects[oldStatusEffect];
        _clientIdByStatusEffects.Remove(oldStatusEffect);
        
        character.StatusEffect_OnClientRemove(clientId);
    }
}