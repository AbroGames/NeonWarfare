using System;
using Godot;
using NeonWarfare.Scenes.World.Infra.Composition;

namespace NeonWarfare.Scenes.World.Infra.ServerNetwork;

/// <summary>
/// Drives the server tick from the physics step. Last in the physics step.
/// </summary>
public partial class ServerTickNode : Node
{
    private Action _runTick;

    public ServerTickNode InitPreReady(Action runTick)
    {
        _runTick = runTick;
        // Last in the physics step: the physics callbacks and every other node's _PhysicsProcess belong to this tick
        // and must run before it is sent
        ProcessPhysicsPriority = int.MaxValue;
        return this;
    }

    public override void _PhysicsProcess(double delta) => _runTick();
}
