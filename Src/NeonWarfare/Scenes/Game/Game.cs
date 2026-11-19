using System;
using Godot;
using GodotBox;
using GodotBox.Godot;
using GodotBox.Godot.Nodes;
using Humanizer;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Game.Starters;
using NeonWarfare.Scenes.Screen.Hud;
using NeonWarfare.Scenes.Screen.ServerHud;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Ports;
using NeonWarfare.Scripts.GlobalServices;
using RepliCAT;
using Serilog;

namespace NeonWarfare.Scenes.Game;

/// <summary>
/// The transport of the session: routes the packets and the connection events between <see cref="Network"/>
/// and the current World, and loops the host's own packets back into its World synchronously, inside the call.
/// </summary>
public partial class Game : Node2D, IClientsConnection, IServerConnection, ILocalPlayerOwner
{
    /// <summary>
    /// The screen of the World, dying with it. <see cref="Hud"/> is created only once this process's player has
    /// joined, so that the UI never sees it offline; the others come with the World.
    /// </summary>
    public enum Screen
    {
        None,
        Hud,
        ServerHud
    }

    private const int ServerPeerId = Consts.Global.ServerId;

    private const string NoWorldLog = "Packet from peer {peerId} dropped: there is no World yet";
    private const string NotFromServerLog = "Packet from peer {peerId} dropped: not from the server";
    private const string BrokenServerPacketLog = "Packet from the server dropped";
    private const string JoinRejectedLog = "The server rejected the join: {reason}";
    private const string HostDisconnectedLog = "The host's own peer is disconnected: the host has no player";
    private const string HudFailedLog = "The Hud failed to be created after the join";
    private const string NoNetworkError = "Peer {0} is not the host's own, but there is no network";
    private const string NoServerError = "There is no server to send to";
    private const string NotInTreeError =
        "Game must be in the tree before Init: WorldPackedScenes fills its scene list in _Ready";

    [Child] private NodeContainer WorldContainer { get; set; }
    [Child] private NodeContainer HudContainer { get; set; }
    [Child] private GamePackedScenes GamePackedScenes { get; set; }
    [Child] private WorldPackedScenes WorldPackedScenes { get; set; }

    private readonly ILogger _log = LogFactory.GetForStatic<Game>();

    private Network _network;
    private World _world;
    private WorldLayer _layers;
    private EntityCatalog _entities;
    private NetMessageCodec _codec;
    private Replicator _replicator;

    // The host is the server and a client in one process, its own peer is the server's
    private int? LocalPeerId => _layers.HasFlag(WorldLayer.Server | WorldLayer.Client)
        ? ServerPeerId
        : null;

    private bool HasWorld => _world != null && IsInstanceValid(_world);

    private bool _isHudDue;

    /// <summary>
    /// A remote client without a World got the server's join snapshot: the World is created from it.
    /// </summary>
    public event Action<byte[]> WorldSnapshotReceivedEvent;

    /// <summary>
    /// The server refused this process's join: a remote client gets it before it has a World, the host from its own
    /// World, inside the tick.
    /// </summary>
    public event Action<JoinRejectReason> JoinRejectedEvent;

    /// <summary>
    /// The server has applied this process's join, and its <see cref="Screen.Hud"/> is created.
    /// </summary>
    public event Action LocalPlayerJoinedEvent;

    public override void _Ready()
    {
        Di.Process(this);
    }

    public void Init(BaseGameStarter gameStarter)
    {
        if (!IsInsideTree()) throw new InvalidOperationException(NotInTreeError);

        // A client writes its join before it has a World, so the protocol objects belong to the session
        _entities = new EntityCatalog(WorldPackedScenes.GetScenesList(), Services.TypesMapping.Types);
        _codec = new NetMessageCodec(Services.TypesMapping, _entities.Descriptors);
        _replicator = new Replicator(Services.TypesMapping);

        gameStarter.Init(this);
    }

    /// <param name="saveFiles">Where a server World saves, <c>null</c> for a remote client.</param>
    /// <param name="localPlayer">The one passed to <see cref="SendJoinRequest"/>, <c>null</c> on a dedicated
    /// server.</param>
    /// <param name="admin"><c>null</c> for a remote client.</param>
    /// <param name="dedicatedServerOwner">Only for a dedicated server.</param>
    public World AddWorld(
        WorldLayer layers, WorldOrigin origin, Screen screen, ISaveFiles saveFiles, LocalPlayer localPlayer,
        WorldAdmin admin, IDedicatedServerOwner dedicatedServerOwner = null)
    {
        var dependencies = new WorldDependencies(
            TimeProvider.System, _codec, _replicator, FrameProvider.Engine, WorldPackedScenes, _entities,
            this, this, saveFiles, localPlayer, admin, dedicatedServerOwner, localPlayer == null ? null : this);
        var world = new World();
        try
        {
            world.InitPreReady(layers, dependencies, origin);
        }
        catch
        {
            // Outside the tree nothing else frees it, nor the entities a snapshot has already spawned in it
            world.Free();
            throw;
        }
        _layers = layers;
        _world = world;
        _world.SetName("World");
        WorldContainer.ChangeStoredNode(_world);

        _isHudDue = screen == Screen.Hud;
        switch (screen)
        {
            case Screen.None:
            case Screen.Hud:
                HudContainer.ClearStoredNode();
                break;
            case Screen.ServerHud:
                ServerHud serverHud = GamePackedScenes.ServerHud.Instantiate<ServerHud>().InitPreReady(_world);
                serverHud.SetName("ServerHud");
                HudContainer.ChangeStoredNode(serverHud);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(screen), screen, null);
        }
        return _world;
    }

    public Network AddNetwork()
    {
        _network?.QueueFree();
        _network = new Network(this);
        _network.PeerConnectedEvent += PeerConnectedEvent;
        _network.PeerDisconnectedEvent += PeerDisconnectedEvent;
        _network.PacketReceivedEvent += PacketReceivedEvent;
        this.AddChildWithName(_network, "Network");
        return _network;
    }

    /// <summary>
    /// The join of this process's player. On the host its own peer first connects, as a remote one would, so it
    /// passes the same gatekeeper.
    /// </summary>
    public void SendJoinRequest(LocalPlayer localPlayer)
    {
        if (LocalPeerId is { } localPeerId)
        {
            _world.OnClientConnected(localPeerId);
        }
        ((IServerConnection) this).Send(_codec.Encode(localPlayer.ToJoinRequest(_codec.ProtocolHash)));
    }

    void ILocalPlayerOwner.Joined()
    {
        if (_isHudDue)
        {
            _isHudDue = false;
            try
            {
                Hud hud = GamePackedScenes.Hud.Instantiate<Hud>().InitPreReady(_world, _world);
                hud.SetName("Hud");
                HudContainer.ChangeStoredNode(hud);
            }
            catch (Exception e)
            {
                // The event dispatch would swallow it and leave the loading screen forever: the join fails instead
                _log.Error(e, HudFailedLog);
                JoinRejectedEvent?.Invoke(JoinRejectReason.InternalError);
                return;
            }
        }
        LocalPlayerJoinedEvent?.Invoke();
    }

    int? IClientsConnection.LocalPeerId => LocalPeerId;

    void IClientsConnection.Send(int peerId, ReadOnlySpan<byte> packet)
    {
        if (peerId == LocalPeerId)
        {
            if (IsJoinRejected(packet))
            {
                RaiseJoinRejected(packet);
            }
            else
            {
                _world.ReceiveFromServer(packet.ToArray());
            }
            return;
        }
        if (_network == null) throw new InvalidOperationException(NoNetworkError.FormatWith(peerId));

        _network.Send(peerId, packet);
    }

    void IClientsConnection.Disconnect(int peerId)
    {
        if (peerId == LocalPeerId)
        {
            // Queued like any other disconnection, so it is processed in the next tick, not inside this one
            _log.Error(HostDisconnectedLog);
            _world.OnClientDisconnected(peerId);
            return;
        }
        if (_network == null) throw new InvalidOperationException(NoNetworkError.FormatWith(peerId));

        _network.Disconnect(peerId);
    }

    void IServerConnection.Send(ReadOnlySpan<byte> packet)
    {
        if (LocalPeerId is { } localPeerId)
        {
            _world.ReceiveFromClient(localPeerId, packet.ToArray());
            return;
        }
        if (_network is not { IsServer: false }) throw new InvalidOperationException(NoServerError);

        _network.Send(ServerPeerId, packet);
    }

    private void PeerConnectedEvent(int peerId)
    {
        if (HasWorld) _world.OnClientConnected(peerId);
    }

    private void PeerDisconnectedEvent(int peerId)
    {
        if (HasWorld) _world.OnClientDisconnected(peerId);
    }

    private void PacketReceivedEvent(int peerId, byte[] packet)
    {
        if (_network.IsServer)
        {
            if (HasWorld)
            {
                _world.ReceiveFromClient(peerId, packet);
            }
            else
            {
                _log.Debug(NoWorldLog, peerId);
            }
            return;
        }
        if (peerId != ServerPeerId)
        {
            _log.Warning(NotFromServerLog, peerId);
            return;
        }
        if (IsJoinRejected(packet))
        {
            try
            {
                RaiseJoinRejected(packet);
            }
            catch (NetMessageFormatException e)
            {
                _log.Error(e, BrokenServerPacketLog);
            }
            return;
        }
        if (!HasWorld)
        {
            // The subscriber creates the World inside the call, before the events packet of the join tick arrives
            if (packet.Length > 0 && packet[0] == (byte) ServerPacketKind.Snapshot)
            {
                WorldSnapshotReceivedEvent?.Invoke(packet);
            }
            else
            {
                _log.Debug(NoWorldLog, peerId);
            }
            return;
        }

        try
        {
            _world.ReceiveFromServer(packet);
        }
        catch (NetMessageFormatException e)
        {
            _log.Error(e, BrokenServerPacketLog);
        }
    }

    private static bool IsJoinRejected(ReadOnlySpan<byte> packet) =>
        packet.Length > 0 && packet[0] == (byte) ServerPacketKind.JoinRejected;

    /// <exception cref="NetMessageFormatException">The rejection is broken.</exception>
    private void RaiseJoinRejected(ReadOnlySpan<byte> packet)
    {
        JoinRejectReason reason = JoinRejectedPacket.Read(packet);
        _log.Error(JoinRejectedLog, reason);
        JoinRejectedEvent?.Invoke(reason);
    }
}
