using Godot;
using Godot.Collections;
using KludgeBox.Godot.Nodes.MpSync;
using NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;
using static Godot.SceneReplicationConfig.ReplicationMode;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Synchronizer;

public partial class CharacterSynchronizer
{

    private CharacterStats _stats;
    private CharacterStatsClient _statsClient;
    
    public bool StatsIsDead { get; private set; }
    public double StatsHp { get; private set; }
    public double StatsDutyHp { get; private set; }
    
    [Export] [Sync(Always)] private Dictionary<CharacterStat, double> _statsValues = new();
    
    private void Stats_OnReady()
    {
        _stats = _character.Stats;
        _statsClient = _character.StatsClient;
    }

    public void Stats_Update(CharacterStat stat, double value)
    {
        _statsValues[stat] = value;
    }

    public double Stats_GetStat(CharacterStat stat)
    {
        return _statsValues[stat];
    }
    
    public void Stats_OnDamage(Character damager, double value, double absorbByArmor, double newHp) => 
        Rpc(MethodName.Stats_OnDamageRpc, damager.GetPath().ToString(), value, absorbByArmor, newHp);
    [Rpc(CallLocal = true, TransferChannel = Consts.TransferChannel.StatsHp)]
    private void Stats_OnDamageRpc(string damager, double value, double absorbByArmor, double newHp)
    {
        StatsHp = newHp;
        _statsClient.OnDamage(GetNodeOrNull<Character>(damager), value, absorbByArmor, newHp);
    }
    
    public void Stats_OnHeal(Character healer, double value, double newHp, double newDutyHp) => 
        Rpc(MethodName.Stats_OnHealRpc, healer.GetPath().ToString(), value, newHp, newDutyHp);
    [Rpc(CallLocal = true, TransferChannel = Consts.TransferChannel.StatsHp)]
    private void Stats_OnHealRpc(string healer, double value, double newHp, double newDutyHp)
    {
        StatsHp = newHp;
        StatsDutyHp = newDutyHp;
        _statsClient.OnHeal(GetNodeOrNull<Character>(healer), value, newHp, newDutyHp);
    } 
    
    public void Stats_OnKill(Character killer) => 
        Rpc(killer.GetPath().ToString(), MethodName.Stats_OnKillRpc);
    [Rpc(CallLocal = true)]
    private void Stats_OnKillRpc(string killer)
    {
        StatsIsDead = true;
        StatsHp = 0;
        _statsClient.OnKill(GetNodeOrNull<Character>(killer));
    }

    public void Stats_OnResurrect(Character resurrector) => 
        Rpc(resurrector.GetPath().ToString(), MethodName.Stats_OnResurrectRpc);
    [Rpc(CallLocal = true)]
    private void Stats_OnResurrectRpc(string resurrector)
    {
        StatsIsDead = false;
        StatsHp = 0;
        _statsClient.OnResurrect(GetNodeOrNull<Character>(resurrector));
    }
}