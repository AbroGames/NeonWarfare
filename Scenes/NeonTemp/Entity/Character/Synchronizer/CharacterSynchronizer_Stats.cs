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
    
    private void InitPostReady_Stats()
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
    
    public void Stats_OnDamage(double value, double newHp) => Rpc(MethodName.Stats_OnDamageRpc, value, newHp);
    [Rpc(CallLocal = true)]
    private void Stats_OnDamageRpc(double value, double newHp)
    {
        StatsHp = newHp;
        _statsClient.OnDamage(value, newHp);
    }
    
    public void Stats_OnHeal(double value, double newHp, double newDutyHp) => Rpc(MethodName.Stats_OnHealRpc, value, newHp, newDutyHp);
    [Rpc(CallLocal = true)]
    private void Stats_OnHealRpc(double value, double newHp, double newDutyHp)
    {
        StatsHp = newHp;
        StatsDutyHp = newDutyHp;
        _statsClient.OnHeal(value, newHp, newDutyHp);
    } 
    
    public void Stats_OnKill() => Rpc(MethodName.Stats_OnKillRpc);
    [Rpc(CallLocal = true)]
    private void Stats_OnKillRpc()
    {
        StatsIsDead = true;
        _statsClient.OnKill();
    }

    public void Stats_OnResurrect() => Rpc(MethodName.Stats_OnResurrectRpc);
    [Rpc(CallLocal = true)]
    private void Stats_OnResurrectRpc()
    {
        StatsIsDead = false;
        _statsClient.OnResurrect();
    }
}