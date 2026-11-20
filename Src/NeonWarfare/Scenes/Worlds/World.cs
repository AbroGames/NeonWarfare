using System;
using System.Reflection;
using Godot;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.Scenes.Worlds.Features.NewWorld;
using NeonWarfare.Scenes.Worlds.Infra.Client;
using NeonWarfare.Scenes.Worlds.Infra.Client.Events;
using NeonWarfare.Scenes.Worlds.Infra.Client.Replication;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Commands;
using NeonWarfare.Scenes.Worlds.Infra.Server.Peers;
using NeonWarfare.Scenes.Worlds.Infra.Server.Saves;
using NeonWarfare.Scenes.Worlds.Infra.Server.Tick;

namespace NeonWarfare.Scenes.Worlds;

public partial class World : Node2D, World.IReader, World.ICommandSender
{
    /// <summary>
    /// What a screen reads from the World.
    /// </summary>
    public interface IReader
    {
        T Get<T>() where T : class;
    }

    /// <summary>
    /// How a screen of a playing peer acts on the World.
    /// </summary>
    public interface ICommandSender
    {
        void Send<TCommand>(TCommand command) where TCommand : Command;
    }

    private const string NotInitializedError = "World is not initialized";
    private const string NoLayerError = "World has no {0} layer";
    private const string NotExposedError = "{0} is not a [Query] or [Presentation] service";
    private const string NotWorldServiceError = "{0} has no world layer attribute";
    private const string EmptyPacketError = "The packet is empty.";
    private const string UnknownKindError = "Unknown server packet kind {0}.";
    private const string SecondSnapshotError = "A snapshot for a World that already has one.";
    private const string JoinRejectedError = "A join rejection is read by Game, not by the World.";

    private ServiceProvider _services;
    private WorldLayer _layers;

    /// <exception cref="NetMessageFormatException">The snapshot of <see cref="WorldOrigin.FromSnapshot"/> is broken:
    /// the World is half built and must be freed.</exception>
    /// <exception cref="SaveFormatException">The save of <see cref="WorldOrigin.FromSave"/> is broken or of another
    /// version (<see cref="SaveVersionMismatchException"/>): the World must be freed.</exception>
    public World InitPreReady(WorldLayer layers, WorldDependencies dependencies, WorldOrigin origin)
    {
        if (_services != null) throw new InvalidOperationException("World is already initialized");
        if (IsInsideTree()) throw new InvalidOperationException("World must be initialized before it enters the tree");

        // Every service is created here, before the world has any entity, so none can read the world in its
        // constructor; the origin fills the world only after that
        _services = new WorldServicesBuilder().Build(layers, dependencies, new WorldRoot(this));
        _layers = layers;

        switch (origin)
        {
            case WorldOrigin.NewWorld newWorld:
                _services.GetRequiredService<NewWorldSimulationFacade>().Create();
                Service<SaveService>().Init(newWorld.SaveFileName);
                break;
            case WorldOrigin.FromSnapshot snapshot:
                Service<StateApplier>().ApplySnapshot(snapshot.Packet);
                break;
            case WorldOrigin.FromSave save:
                Service<SaveLoader>().Load(save.Save);
                Service<SaveService>().Init(save.SaveFileName);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
        }

        if (layers.HasFlag(WorldLayer.Simulation))
        {
            AddChild(new ServerTickNode().InitPreReady(_services.GetRequiredService<ServerTickLoop>().RunTick));
        }
        if (layers.HasFlag(WorldLayer.Server))
        {
            AddChild(new SaveOnExitNode().InitPreReady(Service<SaveService>().SaveOnExit));
        }
        return this;
    }

    /// <summary>
    /// The only way the outside of the World sends commands.
    /// </summary>
    public void Send<TCommand>(TCommand command) where TCommand : Command =>
        Service<PlayerCommandSender>().Send(command);

    /// <summary>
    /// What the outside of the World may read: the Simulation and the network machinery are never handed out.
    /// </summary>
    public T Get<T>() where T : class
    {
        if (ServiceLayer<T>.Attribute is not (QueryAttribute or PresentationAttribute))
        {
            throw new InvalidOperationException(NotExposedError.FormatWith(typeof(T).FullName));
        }

        return Service<T>();
    }

    public void ReceiveFromClient(int peerId, ReadOnlyMemory<byte> packet) =>
        Service<CommandInbox>().EnqueueFromPeer(peerId, packet);

    public void AddClient(int peerId) =>
        Service<PeerGatekeeper>().StartHandshake(peerId);

    public void RemoveClient(int peerId) =>
        Service<CommandInbox>().EnqueuePeerDisconnected(peerId);

    /// <exception cref="NetMessageFormatException">The packet is broken or of an unknown kind.</exception>
    public void ReceiveFromServer(ReadOnlyMemory<byte> packet)
    {
        if (packet.IsEmpty)
        {
            throw new NetMessageFormatException(EmptyPacketError);
        }

        switch ((ServerPacketKind) packet.Span[0])
        {
            case ServerPacketKind.Events:
                Service<EventDispatcher>().DispatchPacket(packet);
                break;
            case ServerPacketKind.State:
                Service<StateApplier>().ApplyPacket(packet);
                break;
            case ServerPacketKind.Snapshot:
                throw new NetMessageFormatException(SecondSnapshotError);
            case ServerPacketKind.JoinRejected:
                throw new NetMessageFormatException(JoinRejectedError);
            default:
                throw new NetMessageFormatException(UnknownKindError.FormatWith(packet.Span[0]));
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPredelete) _services?.Dispose();
    }

    private T Service<T>() where T : class
    {
        if (_services == null) throw new InvalidOperationException(NotInitializedError);
        if (ServiceLayer<T>.Attribute is not { Layer: var layer })
        {
            throw new InvalidOperationException(NotWorldServiceError.FormatWith(typeof(T).FullName));
        }
        if (!_layers.HasFlag(layer)) throw new InvalidOperationException(NoLayerError.FormatWith(layer));

        return _services.GetRequiredService<T>();
    }

    // Reflection once per type rather than on every call
    private static class ServiceLayer<T>
    {
        public static readonly WorldServiceAttribute Attribute =
            typeof(T).GetCustomAttribute<WorldServiceAttribute>(inherit: false);
    }
}
