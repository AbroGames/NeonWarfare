using Godot;
using NeonWarfare.Scripts.Content.LoadingScreen;
using NeonWarfare.Scripts.Service.Settings;

namespace NeonWarfare.Scenes.Game.Starters;

public class ConnectToMultiplayerGameStarter(string host = null, int? port = null) : BaseGameStarter
{
    
    public override void Init(Game game)
    {
        base.Init(game);
        Services.LoadingScreen.SetLoadingScreen(LoadingScreenTypes.Type.Connecting);
        
        PlayerSettings playerSettings = Services.PlayerSettings.GetPlayerSettings();
        game.AddWorld();
        Synchronizer synchronizer = game.AddSynchronizer(playerSettings);
        game.AddHud();
        Network.Network network = game.AddNetwork();
        
        game.GetMultiplayer().ConnectedToServer += synchronizer.StartSyncOnClient;
        game.GetMultiplayer().ConnectionFailed += ConnectionFailedEvent;
        game.GetMultiplayer().ServerDisconnected += ServerDisconnectedEvent;
        
        Error error = network.ConnectToServer(host ?? DefaultHost, port ?? DefaultPort);
        if (error != Error.Ok)
        {
            ConnectionFailedEvent();
        }
    }

    // Failed attempt to connect to the server (did not receive a response from the server within the timeout).
    private void ConnectionFailedEvent()
    {
        Services.MainScene.StartMainMenu();
        //TODO Show message in menu (it is client). Log already has message.
        Services.LoadingScreen.Clear();
    }
    
    // Server disconnected (the connection was successful, but the server disconnected us). This may also happen several hours after the connection.
    private void ServerDisconnectedEvent()
    {
        Services.MainScene.StartMainMenu();
        //TODO Show message in menu (it is client). Log already has message.
        Services.LoadingScreen.Clear();
    }
}