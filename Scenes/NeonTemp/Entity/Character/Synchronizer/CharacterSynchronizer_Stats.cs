using Godot;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Synchronizer;

public partial class CharacterSynchronizer
{

    private CharacterStats _stats;
    private CharacterStatsClient _statsClient;
    
    private void Stats_InitPostReady(Character character)
    {
        _stats = character.Stats;
        _statsClient = character.StatsClient;
    }
    
    //TODO В доку, что здесь не должно быть никакой логики кроме сетевой
    public void Stats_OnStatUpdate(CharacterStat stat, double additive, double multiplicative) => 
        Rpc(MethodName.Stats_OnStatUpdateRpc, (int) stat, additive, multiplicative);
    [Rpc(CallLocal = true, TransferChannel = (int) Consts.TransferChannel.StatsCache)]
    private void Stats_OnStatUpdateRpc(int stat, double additive, double multiplicative)
    {
        if (!Net.IsClient()) return;
        _statsClient.OnStatUpdate((CharacterStat) stat, additive, multiplicative);
    }
    
    public void Stats_OnDamage(Character damager, double value, double absorbByArmor, double newHp) => 
        Rpc(MethodName.Stats_OnDamageRpc, damager.GetPath().ToString(), value, absorbByArmor, newHp);
    [Rpc(CallLocal = true, TransferChannel = (int) Consts.TransferChannel.StatsHp)]
    private void Stats_OnDamageRpc(string damager, double value, double absorbByArmor, double newHp)
    {
        if (!Net.IsClient()) return;
        _statsClient.OnDamage(GetNodeOrNull<Character>(damager), value, absorbByArmor, newHp);
    }
    
    public void Stats_OnHeal(Character healer, double value, double newHp, double newDutyHp) => 
        Rpc(MethodName.Stats_OnHealRpc, healer.GetPath().ToString(), value, newHp, newDutyHp);
    [Rpc(CallLocal = true, TransferChannel = (int) Consts.TransferChannel.StatsHp)]
    private void Stats_OnHealRpc(string healer, double value, double newHp, double newDutyHp)
    {
        if (!Net.IsClient()) return;
        _statsClient.OnHeal(GetNodeOrNull<Character>(healer), value, newHp, newDutyHp);
    } 
    
    public void Stats_OnKill(Character killer) => 
        Rpc(killer.GetPath().ToString(), MethodName.Stats_OnKillRpc);
    [Rpc(CallLocal = true)]
    private void Stats_OnKillRpc(string killer)
    {
        if (!Net.IsClient()) return;
        _statsClient.OnKill(GetNodeOrNull<Character>(killer));
    }

    public void Stats_OnResurrect(Character resurrector) => 
        Rpc(resurrector.GetPath().ToString(), MethodName.Stats_OnResurrectRpc);
    [Rpc(CallLocal = true)]
    private void Stats_OnResurrectRpc(string resurrector)
    {
        if (!Net.IsClient()) return;
        _statsClient.OnResurrect(GetNodeOrNull<Character>(resurrector));
    }
}