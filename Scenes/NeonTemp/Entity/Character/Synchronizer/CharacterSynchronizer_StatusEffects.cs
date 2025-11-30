using System;
using Godot;
using MessagePack;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect;
using NeonWarfare.Scenes.NeonTemp.Service;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Synchronizer;

public partial class CharacterSynchronizer
{

    private CharacterStatusEffects _statusEffects;
    private CharacterStatusEffectsClient _statusEffectsClient;

    private void StatusEffects_OnReady()
    {
        _statusEffects = _character.StatusEffects;
        _statusEffectsClient = _character.StatusEffectsClient;
    }

    public void StatusEffects_OnClientApply(int clientId, AbstractClientStatusEffect clientStatusEffect)
    {
        int typeId = Services.TypesStorage.GetId(clientStatusEffect.GetType());
        byte[] payload = MessagePackSerializer.Serialize(clientStatusEffect.GetType(), clientStatusEffect);
        Rpc(MethodName.StatusEffects_OnClientApplyRpc, clientId, typeId, payload);
    }
    [Rpc(CallLocal = true)]
    private void StatusEffects_OnClientApplyRpc(int clientId, int typeId, byte[] payload)
    {
        Type targetType = Services.TypesStorage.GetType(typeId);
        var clientStatusEffect = (AbstractClientStatusEffect) MessagePackSerializer.Deserialize(targetType, payload);
        _statusEffectsClient.OnAddStatusEffect(clientId, clientStatusEffect);
    }
    
    public void StatusEffects_OnClientRemove(int clientId) => Rpc(MethodName.StatusEffects_OnClientRemoveRpc, clientId);
    [Rpc(CallLocal = true)]
    private void StatusEffects_OnClientRemoveRpc(int clientId) => _statusEffectsClient.OnRemoveStatusEffect(clientId);
}