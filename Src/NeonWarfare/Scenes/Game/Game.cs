using System;
using Godot;
using GodotBox;
using GodotBox.Godot.Nodes;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Game.Starters;
using NeonWarfare.Scenes.Game.Networks;
using NeonWarfare.Scenes.Game.Networks.Transport;
using NeonWarfare.Scenes.Screen.Hud;
using NeonWarfare.Scenes.Screen.ServerHud;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Ports;
using Serilog;

namespace NeonWarfare.Scenes.Game;

/// <summary>
/// One game session: its protocol objects, network, transports and World, see
/// Docs/Startup-flow.md#game-the-session-api.
/// No event is raised once the Game is queued for deletion.
/// </summary>
public partial class Game : Node2D, ILocalPlayerOwner, IServerOwner
{
    // TODO Localization debt: player-visible text must go through Tr(KEY), see Docs/Localization.md.
    private const string ConnectionFailedMessage = "Connection to the server failed";
    private const string DisconnectedFromServerMessage = "Server disconnected";
    private const string ConnectionBrokenMessage = "The data received from the server is broken";
    private const string BrokenWorldMessage = "The world received from the server is broken";
    private const string WorldCreationFailedMessage = "Failed to enter the world received from the server";

    /// <summary>This process's player is online and sees its World.</summary>
    public event Action LocalPlayerJoined;

    /// <summary>
    /// The session of this process's player is over, with a message for the player. The cause is already logged.
    /// </summary>
    public event Action<string> Failed;

    /// <summary>The admin of the server World has left. Raised inside the tick.</summary>
    public event Action AdminLeft;

    [Child] private NodeContainer WorldContainer { get; set; }
    [Child] private NodeContainer HudContainer { get; set; }
    [Child] private GamePackedScenes GamePackedScenes { get; set; }
    [Child] private WorldPackedScenes WorldPackedScenes { get; set; }

    private readonly ILogger _log = LogFactory.GetForStatic<Game>();

    private GameProtocol _protocol;
    private WorldAssembler _assembler;
    private ClientTransport _clientTransport;
    private WorldSetup.RemoteClient _clientSetup;
    private World _world;

    public override void _Ready()
    {
        Di.Process(this);
    }

    public void Init(BaseGameStarter gameStarter)
    {
        if (!IsInsideTree())
        {
            throw new InvalidOperationException(
                "Game must be in the tree before Init: WorldPackedScenes fills its scene list in _Ready");
        }

        _protocol = GameProtocol.Create(WorldPackedScenes, Services.TypesMapping);
        _assembler = new WorldAssembler(_protocol, WorldPackedScenes);
        gameStarter.Start(this);
    }

    /// <summary>Adds the network of a host or a dedicated server. Its World is added next.</summary>
    public Network AddServerNetwork() => AddNetwork();

    /// <summary>
    /// Connects a remote client. The join is sent once connected.
    /// The World is added from the join snapshot, and the <c>Hud</c> right after it, with the join.
    /// </summary>
    public void ConnectToServer(LocalPlayer localPlayer, string host, int port)
    {
        Network network = AddNetwork();
        _clientTransport = new ClientTransport(
            network, _protocol.Codec, _protocol.Hash, localPlayer, this, OnSnapshotReceived);
        _clientTransport.ConnectionBroken += OnConnectionBroken;
        _clientSetup = new WorldSetup.RemoteClient(localPlayer, this);
        network.ConnectionFailed += OnConnectionFailed;
        network.ServerDisconnected += OnServerDisconnected;

        if (network.ConnectToServer(host, port) != Error.Ok)
        {
            OnConnectionFailed();
        }
    }

    /// <summary>
    /// Adds the World of a host without a network. Its own join is sent right away and applied in the next tick.
    /// </summary>
    /// <exception cref="Worlds.Infra.Server.Saves.SaveFormatException">The save of the origin is broken.</exception>
    public World AddSingleplayerWorld(
        ISaveFiles saveFiles, WorldSaveFile saveFile, LocalPlayer localPlayer, WorldOrigin origin)
    {
        return Store(_assembler.Singleplayer(
            new WorldSetup.Host(saveFiles, saveFile, localPlayer, this, this), origin));
    }

    /// <summary>
    /// Adds the World of a host over <paramref name="network"/>. Its own join is sent right away and applied in the
    /// next tick.
    /// </summary>
    /// <exception cref="Worlds.Infra.Server.Saves.SaveFormatException">The save of the origin is broken.</exception>
    public World AddHostWorld(
        Network network, ISaveFiles saveFiles, WorldSaveFile saveFile, LocalPlayer localPlayer, WorldOrigin origin)
    {
        return Store(_assembler.Host(
            new WorldSetup.Host(saveFiles, saveFile, localPlayer, this, this), origin, network));
    }

    /// <exception cref="Worlds.Infra.Server.Saves.SaveFormatException">The save of the origin is broken.</exception>
    public World AddDedicatedWorld(
        Network network, ISaveFiles saveFiles, WorldSaveFile saveFile, WorldAdmin admin, WorldOrigin origin)
    {
        return Store(_assembler.Dedicated(
            new WorldSetup.Dedicated(saveFiles, saveFile, admin, this), origin, network));
    }

    public void ShowServerHud()
    {
        if (_world == null) throw new InvalidOperationException("There is no World to show");

        ServerHud serverHud = GamePackedScenes.ServerHud.Instantiate<ServerHud>().InitPreReady(_world);
        serverHud.SetName("ServerHud");
        HudContainer.ChangeStoredNode(serverHud);
    }

    // The Hud appears only with the join, so the UI never sees this process's player offline.
    void ILocalPlayerOwner.Joined()
    {
        if (IsQueuedForDeletion()) return;
        try
        {
            ShowHud();
        }
        catch (Exception e)
        {
            // The join fails instead of throwing, because the event dispatch would swallow the exception and leave the
            // loading screen forever.
            _log.Error(e, "The Hud failed to be created after the join");
            Fail(JoinRejectedMessage(JoinRejectReason.InternalError));
            return;
        }
        LocalPlayerJoined?.Invoke();
    }

    // The transport has already logged the reason.
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
        if (_world == null) throw new InvalidOperationException("There is no World to show");

        Hud hud = GamePackedScenes.Hud.Instantiate<Hud>().InitPreReady(_world, _world);
        hud.SetName("Hud");
        HudContainer.ChangeStoredNode(hud);
    }

    // Called inside the transport's packet handling, before the events packet of the join tick arrives.
    private void OnSnapshotReceived(ReadOnlyMemory<byte> snapshot)
    {
        if (IsQueuedForDeletion()) return;
        try
        {
            Store(_assembler.RemoteClient(_clientSetup, snapshot, _clientTransport));
        }
        // Any failure ends the session, because the World is already freed and the server sends no second snapshot.
        // Otherwise the connecting screen would stay forever.
        catch (Exception e)
        {
            _log.Error(e, "Creating the World from the join snapshot failed");
            Fail(e is NetMessageFormatException ? BrokenWorldMessage : WorldCreationFailedMessage);
            return;
        }
        _log.Information("Entered the world from the join snapshot");
    }

    // The server did not answer within the timeout. Network has already logged it.
    private void OnConnectionFailed() => Fail(ConnectionFailedMessage);

    private void OnServerDisconnected() => Fail(DisconnectedFromServerMessage);

    // The transport has already logged the packet.
    private void OnConnectionBroken() => Fail(ConnectionBrokenMessage);

    // Does nothing once the Game is queued for deletion, because the multiplayer is still polled until the end of that
    // frame while the session has already been left.
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
        // A server of another build may send a code this build does not know.
        _ => "MESSAGE_MENU__JOIN_REJECTED_UNKNOWN"
    });
}
