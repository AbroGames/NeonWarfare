using System;
using Godot;

namespace NeonWarfare.Scenes.Worlds.Infra.Tick;

/// <summary>
/// Advances the tick of its World last in every physics step: the server tick, or the clock of a remote client.
/// </summary>
public partial class WorldTickNode : Node
{
    private Action _tick;

    public WorldTickNode InitPreReady(Action tick)
    {
        _tick = tick;
        // The tick runs last, so the physics callbacks and every other _PhysicsProcess belong to it.
        ProcessPhysicsPriority = int.MaxValue;
        return this;
    }

    public override void _PhysicsProcess(double delta) => _tick();
}
