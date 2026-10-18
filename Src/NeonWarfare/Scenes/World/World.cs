using System;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.Scenes.World.Composition;
using NeonWarfare.Scenes.World.ServerNetwork;

namespace NeonWarfare.Scenes.World;

public partial class World : Node2D
{
    private ServiceProvider _services;

    public World InitPreReady(WorldLayer layers, WorldDependencies dependencies)
    {
        if (_services != null) throw new InvalidOperationException("World is already initialized");
        if (IsInsideTree()) throw new InvalidOperationException("World must be initialized before it enters the tree");

        _services = new WorldServicesBuilder().Build(layers, dependencies);
        if (layers.HasFlag(WorldLayer.Simulation))
        {
            AddChild(new ServerTickNode().InitPreReady(_services.GetRequiredService<ServerTickLoop>().RunTick));
        }
        return this;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete) _services?.Dispose();
    }
}
