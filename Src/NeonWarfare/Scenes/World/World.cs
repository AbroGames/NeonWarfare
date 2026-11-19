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
        PlayerCommandSender Commands { get; }
    }

    private const string JoinRejectedLog = "The server rejected the join: {reason}";
    private const string NotInitializedError = "World is not initialized";
    private const string NoLayerError = "World has no {0} layer";
    private const string NotExposedError = "{0} is not a [Query] or [Presentation] service";
    private const string NotWorldServiceError = "{0} has no world layer attribute";
    private const string EmptyPacketError = "The packet is empty.";
    private const string UnknownKindError = "Unknown server packet kind {0}.";
    private const string JoinRejectedLengthError = "A join rejection is {0} bytes long, {1} expected.";

    private const int JoinRejectedLength = 2;

    private readonly ILogger _log = LogFactory.GetForStatic<World>();

    private ServiceProvider _services;
    private WorldLayer _layers;

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

    /// <summary>
    /// The only way the outside of the World sends commands.
    /// </summary>
    public PlayerCommandSender Commands => Service<PlayerCommandSender>();

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
            case ServerPacketKind.JoinRejected:
                if (packet.Length != JoinRejectedLength)
                {
                    throw new NetMessageFormatException(
                        JoinRejectedLengthError.FormatWith(packet.Length, JoinRejectedLength));
                }
                //TODO 021 show the reason to the player
                _log.Error(JoinRejectedLog, (JoinRejectReason) packet.Span[1]);
                break;
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
