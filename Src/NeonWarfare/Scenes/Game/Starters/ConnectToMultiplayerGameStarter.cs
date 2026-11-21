using Godot;
using NeonWarfare.Scripts.Content.LoadingScreen;
using NeonWarfare.Scripts.GlobalServices.ResumableGame;

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

    public override void Init(Game game)
    {
        Services.LoadingScreen.SetLoadingScreen(LoadingScreenTypes.Type.Connecting, GoToMenu);
        
        Network.Network network = game.AddNetwork();

        //TODO 021b the World comes from the first snapshot, until then the connecting screen stays
        void ConnectedToServerEvent()
        {
            if (!IsGameAlive(game)) return;
            SendJoinRequest(game);
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

        // Events of Network, which dies with the game, so the handlers need no unsubscribing
        network.ConnectedToServerEvent += ConnectedToServerEvent;
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