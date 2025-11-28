using Godot;
using Godot.Collections;
using KludgeBox.DI.Requests.ParentInjection;
using KludgeBox.Godot.Nodes.MpSync;

namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;

public partial class CharacterStatsNode : Node
{
    
    [Sync] public bool IsDead { get; private set; }
    [Sync] public double Hp { get; private set; } //TODO over hp
    [Sync] public double DutyHp { get; private set; }
    [Sync] public Dictionary<CharacterStat, double> StatsValues = new();

    [Parent] private Character _character;
    private readonly StatModifiersContainer<CharacterStat> _statModifiersContainer = new();

    public override void _Ready()
    {
        Di.Process(this);
    }

    public StatModifiersContainer<CharacterStat> GetContainer()
    {
        return _statModifiersContainer;
    }
}