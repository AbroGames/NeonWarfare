using System;
using System.Reflection;
using Godot;
using Humanizer;
using KludgeBox.Logging;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.Scenes.World.Features.NewWorld;
using NeonWarfare.Scenes.World.Infra.ClientNetwork;
using NeonWarfare.Scenes.World.Infra.ClientReplication;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Entities;
using NeonWarfare.Scenes.World.Infra.Protocol;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Commands;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Peers;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Saves;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Tick;
using Serilog;

namespace NeonWarfare.Scenes.World;

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

    private const string JoinRejectedLog = "The server rejected the join: {reason}";
    private const string NotInitializedError = "World is not initialized";
    private const string NoLayerError = "World has no {0} layer";
    private const string NotExposedError = "{0} is not a [Query] or [Presentation] service";
    private const string NotWorldServiceError = "{0} has no world layer attribute";
    private const string EmptyPacketError = "The packet is empty.";
    private const string UnknownKindError = "Unknown server packet kind {0}.";
    private const string SecondSnapshotError = "A snapshot for a World that already has one.";
    private const string JoinRejectedLengthError = "A join rejection is {0} bytes long, {1} expected.";

    private const int JoinRejectedLength = 2;

    private readonly ILogger _log = LogFactory.GetForStatic<World>();

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
        return this;
    }

    /// <summary>
    /// After every save file the server writes, with its name. Only on a World with
    /// <see cref="WorldLayer.ServerNetwork"/>.
    /// </summary>
    public event Action<string> SavedEvent
    {
        add => Service<SaveService>().SavedEvent += value;
        remove => Service<SaveService>().SavedEvent -= value;
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

    public void OnClientConnected(int peerId) =>
        Service<PeerGatekeeper>().OnPeerConnected(peerId);

    public void OnClientDisconnected(int peerId) =>
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
                if (packet.Length != JoinRejectedLength)
                {
                    throw new NetMessageFormatException(
                        JoinRejectedLengthError.FormatWith(packet.Length, JoinRejectedLength));
                }
                //TODO 031 show the reason to the player
                _log.Error(JoinRejectedLog, (JoinRejectReason) packet.Span[1]);
                break;
            default:
                throw new NetMessageFormatException(UnknownKindError.FormatWith(packet.Span[0]));
        }
    }

    public override void _Notification(int what)
    {
        // Between ticks: Quit() only sets a flag, the tree is torn down after the physics step, so the baselines are
        // the end of the last tick. The services live until predelete, which comes after the exit
        if (what == NotificationExitTree && _layers.HasFlag(WorldLayer.ServerNetwork))
        {
            Service<SaveService>().SaveOnExit();
        }
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
