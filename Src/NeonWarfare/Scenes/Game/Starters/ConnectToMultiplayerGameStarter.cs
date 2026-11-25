using System;
using Godot;
using KludgeBox.Logging;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Features.Players;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Protocol;
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
    ) : BaseGameStarter
{
    
    // TODO Localization debt: player-visible text must go through Tr(KEY), see Docs/Localization.md
    private const string ConnectionFailedMessage = "Connection to the server failed";
    private const string DisconnectedFromServerMessage = "Server disconnected";
    private const string BrokenWorldMessage = "The world received from the server is broken";
    private const string WorldCreationFailedMessage = "Failed to enter the world received from the server";
    private const string BrokenSnapshotLog = "The join snapshot from the server is broken";
    private const string WorldCreationFailedLog = "Creating the World from the join snapshot failed";

    private readonly ILogger _log = LogFactory.GetForStatic<ConnectToMultiplayerGameStarter>();

    public override void Init(Game game)
    {
        Services.LoadingScreen.SetLoadingScreen(LoadingScreenTypes.Type.Connecting, GoToMenu);
        
        Network.Network network = game.AddNetwork();
        LocalPlayer localPlayer = ReadLocalPlayer();

        // The World comes from the first snapshot, until then the connecting screen stays
        void ConnectedToServerEvent()
        {
            if (!IsGameAlive(game)) return;
            game.SendJoinRequest(localPlayer);
        }
        
        void WorldSnapshotReceivedEvent(byte[] snapshot)
        {
            if (!IsGameAlive(game)) return;
            try
            {
                game.AddWorld(
                    WorldLayer.Client, new WorldOrigin.FromSnapshot(snapshot), Game.Screen.Hud, saveFiles: null,
                    localPlayer);
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
            Services.LoadingScreen.Clear();
        }

        // Failed attempt to connect to the server (did not receive a response from the server within the timeout).
        void ConnectionFailedEvent()
        {
            if (!IsGameAlive(game)) return;
            GoToMenuAndShowError(ConnectionFailedMessage);
        }
    
        // Server disconnected (the connection was successful, but the server disconnected us).
        // This may also happen several hours after the connection.
        void ServerDisconnectedEvent()
        {
            if (!IsGameAlive(game)) return;
            GoToMenuAndShowError(DisconnectedFromServerMessage);
        }

        // Events of Network and Game, which die with the game, so the handlers need no unsubscribing
        network.ConnectedToServerEvent += ConnectedToServerEvent;
        game.WorldSnapshotReceivedEvent += WorldSnapshotReceivedEvent;
        network.ConnectionFailedEvent += ConnectionFailedEvent;
        network.ServerDisconnectedEvent += ServerDisconnectedEvent;

        if (mustSetLastGame)
        {
            var lastGame = ResumableGame.GetConnectToServer(host ?? DefaultHost, port ?? DefaultPort);
            SetLastGame(lastGame);
        }

        Error error = network.ConnectToServer(host ?? DefaultHost, port ?? DefaultPort);
        if (error != Error.Ok)
        {
            ConnectionFailedEvent();
        }
    }
    
    private bool IsGameAlive(Game game)
    {
        return GodotObject.IsInstanceValid(game) && !game.IsQueuedForDeletion();
    }
}