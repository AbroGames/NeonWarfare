using System;
using Godot;
using NeonWarfare.Scenes.Worlds.Infra.Composition;

namespace NeonWarfare.Scenes.Worlds.Infra.Server.Tick;

/// <summary>Runs the server tick last in every physics step.</summary>
public partial class ServerTickNode : Node
{
    private Action _runTick;

    public ServerTickNode InitPreReady(Action runTick)
    {
        _runTick = runTick;
        // The tick runs last, so the physics callbacks and every other _PhysicsProcess belong to it.
        ProcessPhysicsPriority = int.MaxValue;
        return this;
    }

    public override void _PhysicsProcess(double delta) => _runTick();
}
