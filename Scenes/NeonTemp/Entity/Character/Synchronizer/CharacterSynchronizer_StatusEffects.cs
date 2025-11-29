using Godot;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.StatusEffect;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Synchronizer;

public partial class CharacterSynchronizer
{

    private CharacterStatusEffects _statusEffects;
    private CharacterStatusEffectsClient _statusEffectsClient;

    private void InitPostReady_StatusEffects()
    {
        _statusEffects = _character.StatusEffects;
        _statusEffectsClient = _character.StatusEffectsClient;
    }
    
    public void StatusEffect_OnClientApply(int clientId, int typeId, byte[] payload) => 
        Rpc(MethodName.StatusEffect_OnClientApplyRpc, clientId, typeId, payload);
    [Rpc(CallLocal = true)]
    private void StatusEffect_OnClientApplyRpc(int clientId, int typeId, byte[] payload) => 
        _statusEffectsClient.OnAddStatusEffect(clientId, typeId, payload);

    public void StatusEffect_OnClientRemove(int clientId) => Rpc(MethodName.StatusEffect_OnClientRemoveRpc, clientId);
    [Rpc(CallLocal = true)]
    private void StatusEffect_OnClientRemoveRpc(int clientId) => _statusEffectsClient.OnRemoveStatusEffect(clientId);
}