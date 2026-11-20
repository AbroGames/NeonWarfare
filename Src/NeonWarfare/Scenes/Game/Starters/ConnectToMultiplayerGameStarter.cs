using System;
using Godot;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Ports;
using NeonWarfare.Scripts.Content.LoadingScreen;
using NeonWarfare.Scripts.GlobalServices.ResumableGame;
using Serilog;

namespace NeonWarfare.Scenes.Game.Starters;

/// <summary>
/// An ENet client connecting to a server in another process or machine.
/// </summary>
public class ConnectToMultiplayerGameStarter(
    string host,
    int? port,
    bool mustSetLastGame
    ) : BaseGameStarter, ILocalPlayerOwner
{
    // TODO Localization debt: player-visible text must go through Tr(KEY), see Docs/Localization.md
    private const string ConnectionFailedMessage = "Connection to the server failed";
    private const string DisconnectedFromServerMessage = "Server disconnected";
    private const string BrokenWorldMessage = "The world received from the server is broken";
    private const string WorldCreationFailedMessage = "Failed to enter the world received from the server";
    private const string BrokenSnapshotLog = "The join snapshot from the server is broken";
    private const string WorldCreationFailedLog = "Creating the World from the join snapshot failed";
    private const string EnteredWorldLog = "Entered the world from the join snapshot";

    private readonly ILogger _log = LogFactory.GetForStatic<ConnectToMultiplayerGameStarter>();

    private Game _game;

    public override void Init(Game game)
    {
        Connect(game, ReadLocalPlayer());
    }

    public void Joined() => ShowHudOnJoined(_game);

    public void JoinRejected(JoinRejectReason reason) => GoToMenuOnJoinRejected(_game, reason);

    protected void Connect(Game game, LocalPlayer localPlayer)
    {
        _game = game;
        Services.LoadingScreen.SetLoadingScreen(LoadingScreenTypes.Type.Connecting, GoToMenu);

        // The World comes from the first snapshot and the Hud with the join right after it, until then the connecting
        // screen stays
        void OnWorldSnapshotReceived(byte[] snapshot)
        {
            if (!IsGameAlive(game)) return;
            try
            {
                game.AddClientWorld(snapshot);
            }
            catch (NetMessageFormatException e)
            {
                // The server sends no second snapshot, so waiting for one is pointless
                _log.Error(e, BrokenSnapshotLog);
                GoToMenuAndShowError(BrokenWorldMessage);
                return;
            }
            catch (Exception e)
            {
                // The World is already freed, so without the menu the connecting screen would stay forever
                _log.Error(e, WorldCreationFailedLog);
                GoToMenuAndShowError(WorldCreationFailedMessage);
                return;
            }
            _log.Information(EnteredWorldLog);
        }

        // Failed attempt to connect to the server (did not receive a response from the server within the timeout).
        void OnConnectionFailed()
        {
            if (!IsGameAlive(game)) return;
            GoToMenuAndShowError(ConnectionFailedMessage);
        }
    
        // Server disconnected (the connection was successful, but the server disconnected us).
        // This may also happen several hours after the connection.
        void OnServerDisconnected()
        {
            if (!IsGameAlive(game)) return;
            GoToMenuAndShowError(DisconnectedFromServerMessage);
        }

        Network network = game.AddClientNetwork(localPlayer, this, OnWorldSnapshotReceived);
        // Events of Network, which dies with the game, so the handlers need no unsubscribing
        network.ConnectionFailed += OnConnectionFailed;
        network.ServerDisconnected += OnServerDisconnected;

        if (mustSetLastGame)
        {
            var lastGame = ResumableGame.GetConnectToServer(host ?? DefaultHost, port ?? DefaultPort);
            SetLastGame(lastGame);
        }

        Error error = network.ConnectToServer(host ?? DefaultHost, port ?? DefaultPort);
        if (error != Error.Ok)
        {
            OnConnectionFailed();
        }
    }
}