using System;
using System.Diagnostics;
using System.Reflection;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.Scenes.Worlds.Features.NewWorld;
using NeonWarfare.Scenes.Worlds.Infra.Client;
using NeonWarfare.Scenes.Worlds.Infra.Client.Events;
using NeonWarfare.Scenes.Worlds.Infra.Client.Replication;
using NeonWarfare.Scenes.Worlds.Infra.Client.Tick;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Commands;
using NeonWarfare.Scenes.Worlds.Infra.Server.Peers;
using NeonWarfare.Scenes.Worlds.Infra.Server.Saves;
using NeonWarfare.Scenes.Worlds.Infra.Server.Tick;
using NeonWarfare.Scenes.Worlds.Infra.Tick;

namespace NeonWarfare.Scenes.Worlds;

/// <summary>The composition root of one game world, see Docs/World.md#the-world-node.</summary>
public partial class World
    : Node2D, World.IReader, World.ICommandSender, World.IServerEndpoint, World.IHostEndpoint, World.IClientEndpoint
{
    /// <summary>What a screen reads from the World.</summary>
    public interface IReader
    {
        /// <summary>
        /// Hands out a <c>[Query]</c> or <c>[Presentation]</c> service.
        /// The Simulation and the network machinery are never handed out.
        /// </summary>
        T Get<T>() where T : class;
    }

    /// <summary>How the screen of a playing process acts on the World.</summary>
    public interface ICommandSender
    {
        /// <summary>The only way to send a command from outside the World.</summary>
        void Send<TCommand>(TCommand command) where TCommand : Command;
    }

    /// <summary>What a server World receives from its peers.</summary>
    public interface IServerEndpoint
    {
        void StartHandshake(int peerId);

        void QueueDisconnection(int peerId);

        void ReceiveFromClient(int peerId, ReadOnlyMemory<byte> packet);
    }

    /// <summary>What the host's World receives as its own peer.</summary>
    public interface IHostEndpoint
    {
        /// <summary>Only the events: the host's Simulation has written the state already.</summary>
        /// <exception cref="NetMessageFormatException">The section is broken.</exception>
        void ReceiveOwnEvents(ReadOnlyMemory<byte> events);
    }

    /// <summary>What a remote client World receives from the server.</summary>
    public interface IClientEndpoint
    {
        /// <summary>
        /// Syncs the clock to the tick before anything else, so every event handler sees at least that tick.
        /// Applies the state section before it dispatches the events section, so an event handler sees the models at
        /// the end of the tick. An empty section is absent.
        /// </summary>
        /// <exception cref="NetMessageFormatException">A section is broken.</exception>
        void ReceiveTickUpdate(long tick, ReadOnlyMemory<byte> state, ReadOnlyMemory<byte> events);
    }

    private ServiceProvider _services;

    /// <exception cref="NetMessageFormatException">The snapshot of <see cref="WorldOrigin.FromSnapshot"/> is broken.
    /// The World is half built and must be freed.</exception>
    /// <exception cref="SaveFormatException">The save of <see cref="WorldOrigin.FromSave"/> is broken or of another
    /// version (<see cref="SaveVersionMismatchException"/>). The World must be freed.</exception>
    public World InitPreReady(WorldSetup setup, WorldDependencies dependencies, WorldOrigin origin)
    {
        if (_services != null) throw new InvalidOperationException("World is already initialized");
        if (IsInsideTree()) throw new InvalidOperationException("World must be initialized before it enters the tree");

        // Every service is created before the world has any entity, so no constructor can read the world.
        // The origin fills the world only after that.
        _services = new WorldServicesBuilder().Build(setup, dependencies, new WorldRoot(this));

        switch (origin)
        {
            case WorldOrigin.NewWorld:
                _services.GetRequiredService<NewWorldSimulationFacade>().Create();
                break;
            case WorldOrigin.FromSnapshot snapshot:
                Service<ClientTickClock>().Sync(snapshot.Tick);
                Service<StateApplier>().ApplySnapshot(snapshot.Records);
                break;
            case WorldOrigin.FromSave save:
                Service<SaveLoader>().Load(save.Save);
                break;
            default:
                throw new UnreachableException();
        }

        if (setup.Layers.HasFlag(WorldLayer.Simulation))
        {
            AddChild(new WorldTickNode().InitPreReady(_services.GetRequiredService<ServerTickLoop>().RunTick));
        }
        if (setup.Layers.HasFlag(WorldLayer.ClientReplication))
        {
            AddChild(new WorldTickNode().InitPreReady(Service<ClientTickClock>().Advance));
        }
        if (setup.Layers.HasFlag(WorldLayer.Server))
        {
            AddChild(new SaveOnExitNode().InitPreReady(Service<SaveService>().SaveOnExit));
        }
        return this;
    }

    void ICommandSender.Send<TCommand>(TCommand command) => Service<PlayerCommandSender>().Send(command);

    T IReader.Get<T>()
    {
        if (ServiceLayer<T>.Attribute is not (QueryAttribute or PresentationAttribute))
        {
            throw new InvalidOperationException($"{typeof(T).FullName} is not a [Query] or [Presentation] service");
        }

        return Service<T>();
    }

    void IServerEndpoint.StartHandshake(int peerId) => Service<PeerGatekeeper>().StartHandshake(peerId);

    void IServerEndpoint.QueueDisconnection(int peerId) => Service<CommandInbox>().EnqueuePeerDisconnected(peerId);

    void IServerEndpoint.ReceiveFromClient(int peerId, ReadOnlyMemory<byte> packet) =>
        Service<CommandInbox>().EnqueueFromPeer(peerId, packet);

    void IHostEndpoint.ReceiveOwnEvents(ReadOnlyMemory<byte> events) => Service<EventDispatcher>().Dispatch(events);

    void IClientEndpoint.ReceiveTickUpdate(long tick, ReadOnlyMemory<byte> state, ReadOnlyMemory<byte> events)
    {
        Service<ClientTickClock>().Sync(tick);
        if (!state.IsEmpty) Service<StateApplier>().ApplyState(state);
        if (!events.IsEmpty) Service<EventDispatcher>().Dispatch(events);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete) _services?.Dispose();
    }

    private T Service<T>() where T : class
    {
        if (_services == null) throw new InvalidOperationException("World is not initialized");
        return _services.GetRequiredService<T>();
    }

    // Caches the layer attribute, so reflection runs once per type.
    private static class ServiceLayer<T>
    {
        public static readonly WorldServiceAttribute Attribute =
            typeof(T).GetCustomAttribute<WorldServiceAttribute>(inherit: false);
    }
}
