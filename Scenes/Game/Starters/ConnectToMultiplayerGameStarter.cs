using Godot;
using NeonWarfare.Scripts.Content.LoadingScreen;

namespace NeonWarfare.Scenes.Game.Starters;

public class ConnectToMultiplayerGameStarter(string host = null, int? port = null) : BaseGameStarter
{
    
    private const string ConnectionFailedMessage = "Connection to the server failed";
    private const string DisconnectedFromServerMessage = "Server disconnected";
    
    public override void Init(Game game)
    {
        base.Init(game);
        Services.LoadingScreen.SetLoadingScreen(LoadingScreenTypes.Type.Connecting);
        
        Network.Network network = game.AddNetwork();
        World.World world = game.AddWorld();
        game.AddHud();

        // Use inner function for detach this function after connecting to server,
        // otherwise we have memory leak for world.SynchronizerService
        void ConnectedToServerEvent()
        {
            ClientStartWorld(world);
            game.GetMultiplayer().ConnectedToServer -= ConnectedToServerEvent;
        }

        game.GetMultiplayer().ConnectedToServer += ConnectedToServerEvent;
        game.GetMultiplayer().ConnectionFailed += ConnectionFailedEvent;
        game.GetMultiplayer().ServerDisconnected += ServerDisconnectedEvent;
        
        Error error = network.ConnectToServer(host ?? DefaultHost, port ?? DefaultPort);
        if (error != Error.Ok)
        {
            ConnectionFailedEvent();
        }
    }
    
    // Failed attempt to connect to the server (did not receive a response from the server within the timeout).
    private void ConnectionFailedEvent() => GoToMenuAndShowError(ConnectionFailedMessage);
    
    // Server disconnected (the connection was successful, but the server disconnected us). This may also happen several hours after the connection.
    private void ServerDisconnectedEvent() => GoToMenuAndShowError(DisconnectedFromServerMessage);
}