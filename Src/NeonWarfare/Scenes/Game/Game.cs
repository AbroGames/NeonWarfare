using System;
using Godot;
using GodotBox;
using GodotBox.Godot.Nodes;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Game.Starters;
using NeonWarfare.Scenes.Game.Transport;
using NeonWarfare.Scenes.Screen.Hud;
using NeonWarfare.Scenes.Screen.ServerHud;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Ports;
using Serilog;

namespace NeonWarfare.Scenes.Game;

/// <summary>
/// One game session: its scene, its protocol objects, its network, transport and World. The starter brings it up
/// through the calls below; each mode has its own, so a session is built only in the order its mode allows. What
/// happens to the session later is reported through the events, never once the Game is queued for deletion.
/// </summary>
public partial class Game : Node2D, ILocalPlayerOwner, IServerOwner
{
    private const string NotInTreeError =
        "Game must be in the tree before Init: WorldPackedScenes fills its scene list in _Ready";
    private const string NoServerNetworkError = "A dedicated server needs AddServerNetwork first";
    private const string NoWorldError = "There is no World to show";

    // TODO Localization debt: player-visible text must go through Tr(KEY), see Docs/Localization.md
    private const string ConnectionFailedMessage = "Connection to the server failed";
    private const string DisconnectedFromServerMessage = "Server disconnected";
    private const string BrokenWorldMessage = "The world received from the server is broken";
    private const string WorldCreationFailedMessage = "Failed to enter the world received from the server";
    private const string BrokenSnapshotLog = "The join snapshot from the server is broken";
    private const string WorldCreationFailedLog = "Creating the World from the join snapshot failed";
    private const string EnteredWorldLog = "Entered the world from the join snapshot";
    private const string HudFailedLog = "The Hud failed to be created after the join";

    /// <summary>
    /// This process's player is online and sees its World.
    /// </summary>
    public event Action LocalPlayerJoined;

    /// <summary>
    /// The session of this process's player is over, with a message for it: the join was rejected, the World or its
    /// <c>Hud</c> failed to be created, the connection failed or the server disconnected. Already logged.
    /// </summary>
    public event Action<string> Failed;

    /// <summary>
    /// The admin of the server World has left. Raised inside the tick.
    /// </summary>
    public event Action AdminLeft;

    [Child] private NodeContainer WorldContainer { get; set; }
    [Child] private NodeContainer HudContainer { get; set; }
    [Child] private GamePackedScenes GamePackedScenes { get; set; }
    [Child] private WorldPackedScenes WorldPackedScenes { get; set; }

    private readonly ILogger _log = LogFactory.GetForStatic<Game>();

    private GameProtocol _protocol;
    private WorldAssembler _assembler;
    private Network _serverNetwork;
    private ClientTransport _clientTransport;
    private WorldSetup.RemoteClient _clientSetup;
    private World _world;

    public override void _Ready()
    {
        Di.Process(this);
    }

    public void Init(BaseGameStarter gameStarter)
    {
        if (!IsInsideTree()) throw new InvalidOperationException(NotInTreeError);

        _protocol = GameProtocol.Create(WorldPackedScenes);
        _assembler = new WorldAssembler(_protocol, WorldPackedScenes);
        gameStarter.Start(this);
    }

    /// <summary>
    /// The network of a host or a dedicated server; its World is added next.
    /// </summary>
    public Network AddServerNetwork()
    {
        _serverNetwork = AddNetwork();
        return _serverNetwork;
    }

    /// <summary>
    /// A remote client: the join is sent once connected, the World is added from the join snapshot and the
    /// <c>Hud</c> with the join right after it.
    /// </summary>
    public void ConnectToServer(LocalPlayer localPlayer, string host, int port)
    {
        Network network = AddNetwork();
        _clientTransport = new ClientTransport(network, _protocol.Codec, localPlayer, this, OnSnapshotReceived);
        _clientSetup = new WorldSetup.RemoteClient(localPlayer, this);
        network.ConnectionFailed += OnConnectionFailed;
        network.ServerDisconnected += OnServerDisconnected;

        if (network.ConnectToServer(host, port) != Error.Ok)
        {
            OnConnectionFailed();
        }
    }

    /// <summary>
    /// Single player, or a host over <see cref="AddServerNetwork"/>. The host's own join is sent right away and
    /// applied in the next tick.
    /// </summary>
    /// <exception cref="Worlds.Infra.Server.Saves.SaveFormatException">The save of the origin is broken.</exception>
    public World AddHostWorld(ISaveFiles saveFiles, LocalPlayer localPlayer, WorldOrigin origin)
    {
        return Store(_assembler.Host(new WorldSetup.Host(saveFiles, localPlayer, this, this), origin, _serverNetwork));
    }

    /// <exception cref="Worlds.Infra.Server.Saves.SaveFormatException">The save of the origin is broken.</exception>
    public World AddDedicatedWorld(ISaveFiles saveFiles, WorldAdmin admin, WorldOrigin origin)
    {
        if (_serverNetwork == null) throw new InvalidOperationException(NoServerNetworkError);

        return Store(_assembler.Dedicated(new WorldSetup.Dedicated(saveFiles, admin, this), origin, _serverNetwork));
    }

    public void ShowServerHud()
    {
        if (_world == null) throw new InvalidOperationException(NoWorldError);

        ServerHud serverHud = GamePackedScenes.ServerHud.Instantiate<ServerHud>().InitPreReady(_world);
        serverHud.SetName("ServerHud");
        HudContainer.ChangeStoredNode(serverHud);
    }

    // The Hud appears only with the join: the UI never sees this process's player offline
    void ILocalPlayerOwner.Joined()
    {
        if (IsQueuedForDeletion()) return;
        try
        {
            ShowHud();
        }
        catch (Exception e)
        {
            // The event dispatch would swallow it and leave the loading screen forever: the join fails instead
            _log.Error(e, HudFailedLog);
            Fail(JoinRejectedMessage(JoinRejectReason.InternalError));
            return;
        }
        LocalPlayerJoined?.Invoke();
    }

    // The transport has already logged the reason
    void ILocalPlayerOwner.JoinRejected(JoinRejectReason reason) => Fail(JoinRejectedMessage(reason));

    void IServerOwner.AdminLeft()
    {
        if (IsQueuedForDeletion()) return;
        AdminLeft?.Invoke();
    }

    private Network AddNetwork()
    {
        var network = new Network(this);
        this.AddChildWithName(network, "Network");
        return network;
    }

    private World Store(World world)
    {
        _world = world;
        _world.SetName("World");
        WorldContainer.ChangeStoredNode(_world);
        HudContainer.ClearStoredNode();
        return world;
    }

    private void ShowHud()
    {
        if (_world == null) throw new InvalidOperationException(NoWorldError);

        Hud hud = GamePackedScenes.Hud.Instantiate<Hud>().InitPreReady(_world, _world);
        hud.SetName("Hud");
        HudContainer.ChangeStoredNode(hud);
    }

    // Called inside the transport's packet handling, before the events packet of the join tick arrives
    private void OnSnapshotReceived(ReadOnlyMemory<byte> snapshot)
    {
        if (IsQueuedForDeletion()) return;
        try
        {
            Store(_assembler.RemoteClient(_clientSetup, snapshot, _clientTransport));
        }
        catch (NetMessageFormatException e)
        {
            // The server sends no second snapshot, so waiting for one is pointless
            _log.Error(e, BrokenSnapshotLog);
            Fail(BrokenWorldMessage);
            return;
        }
        catch (Exception e)
        {
            // The World is already freed, so without a failure the connecting screen would stay forever
            _log.Error(e, WorldCreationFailedLog);
            Fail(WorldCreationFailedMessage);
            return;
        }
        _log.Information(EnteredWorldLog);
    }

    // No answer from the server within the timeout; Network has already logged it
    private void OnConnectionFailed() => Fail(ConnectionFailedMessage);

    // Can arrive even hours into the game
    private void OnServerDisconnected() => Fail(DisconnectedFromServerMessage);

    // The multiplayer is still polled until the end of the frame the Game is queued for deletion in, and the
    // session has already been left by then
    private void Fail(string message)
    {
        if (IsQueuedForDeletion()) return;
        Failed?.Invoke(message);
    }

    private static string JoinRejectedMessage(JoinRejectReason reason) => Services.I18N.Tr(reason switch
    {
        JoinRejectReason.ProtocolMismatch => "MESSAGE_MENU__JOIN_REJECTED_PROTOCOL_MISMATCH",
        JoinRejectReason.InvalidUid => "MESSAGE_MENU__JOIN_REJECTED_INVALID_UID",
        JoinRejectReason.InvalidNick => "MESSAGE_MENU__JOIN_REJECTED_INVALID_NICK",
        JoinRejectReason.InvalidColor => "MESSAGE_MENU__JOIN_REJECTED_INVALID_COLOR",
        JoinRejectReason.UidInUse => "MESSAGE_MENU__JOIN_REJECTED_UID_IN_USE",
        JoinRejectReason.InternalError => "MESSAGE_MENU__JOIN_REJECTED_INTERNAL_ERROR",
        // A server of another build may send a code this one does not know
        _ => "MESSAGE_MENU__JOIN_REJECTED_UNKNOWN"
    });
}
