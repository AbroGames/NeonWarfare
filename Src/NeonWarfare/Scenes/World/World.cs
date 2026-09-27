using System;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.Scenes.World.Composition;
using NeonWarfare.Scenes.World.Models;

namespace NeonWarfare.Scenes.World;

public partial class World : Node2D
{

    private ServiceProvider _services;

    public World InitPreReady(WorldServiceGroups groups, PersistenceModel persistence, SessionModel session)
    {
        if (_services != null) throw new InvalidOperationException("World is already initialized");
        if (IsInsideTree()) throw new InvalidOperationException("World must be initialized before it enters the tree");

        var dependencies = new WorldDependencies(TimeProvider.System, persistence, session);
        _services = new WorldServicesBuilder().Build(groups, dependencies);
        return this;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete) _services?.Dispose();
    }
}
