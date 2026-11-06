using System;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.Scenes.World.Features.NewWorld;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Entities;
using NeonWarfare.Scenes.World.Infra.ServerNetwork;

namespace NeonWarfare.Scenes.World;

public partial class World : Node2D
{
    private ServiceProvider _services;

    public World InitPreReady(WorldLayer layers, WorldDependencies dependencies, WorldOrigin origin)
    {
        if (_services != null) throw new InvalidOperationException("World is already initialized");
        if (IsInsideTree()) throw new InvalidOperationException("World must be initialized before it enters the tree");
        
        // Every service is created here, before the world has any entity, so none can read the world in its
        // constructor; the origin fills the world only after that
        _services = new WorldServicesBuilder().Build(layers, dependencies, new WorldRoot(this));
        
        switch (origin)
        {
            case WorldOrigin.NewWorld:
                _services.GetRequiredService<NewWorldSimulationFacade>().Create();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
        }

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
