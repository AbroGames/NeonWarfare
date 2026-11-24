using System;
using Godot;
using GodotBox;
using GodotBox.Godot.Nodes;
using NeonWarfare.Scenes.Game;
using NeonWarfare.Scenes.Game.Starters;
using NeonWarfare.Scenes.Screen.Menu.MainMenu;

namespace NeonWarfare.Scripts.GlobalServices;

public class MainSceneService
{
    
    private NodeContainer _mainSceneContainer;
    private PackedScene _gamePackedScene;
    private PackedScene _mainMenuPackedScene;

    public void Init(NodeContainer mainSceneContainer, PackedScene gamePackedScene, PackedScene mainMenuPackedScene)
    {
        _mainSceneContainer = mainSceneContainer;
        _gamePackedScene = gamePackedScene;
        _mainMenuPackedScene = mainMenuPackedScene;
    }
    
    /// <summary>
    /// Clears the loading screen once the menu is shown.
    /// </summary>
    public void StartMainMenu()
    {
        LeaveGame(() => ShowMainMenu());
    }

    /// <inheritdoc cref="StartMainMenu()"/>
    public void StartMainMenu(string message)
    {
        LeaveGame(() =>
        {
            MainMenu mainMenu = ShowMainMenu();

            // We must call this section after adding MainMenu to tree, because otherwise we can't
            // access mainMenu.PagesProvider property
            mainMenu.PushPage(mainMenu.PagesProvider.PrepareMessagePage(message));
        });
    }
    
    public void StartSingleplayerGame(string saveFileName)
    {
        Game game = _gamePackedScene.Instantiate<Game>();
        game.SetName("Game");
        _mainSceneContainer.ChangeStoredNode(game);
        
        game.Init(new SingleplayerGameStarter(
            saveFileName: saveFileName));
    }
    
    public void ConnectToMultiplayerGame(string host = null, int? port = null)
    {
        Game game = _gamePackedScene.Instantiate<Game>();
        game.SetName("Game");
        _mainSceneContainer.ChangeStoredNode(game);
        
        game.Init(new ConnectToMultiplayerGameStarter(host: host, port: port));
    }
    
    /// <summary>
    /// Start a new server and connect to them. Use in the client process.
    /// </summary>
    /// <param name="saveFileName">Name of the save file in the folder with saves, required non-null</param>
    /// <param name="port">Port number on which the server will listen to.</param>
    /// <param name="createDedicatedServerProcess">If true, create a new OS process running
    /// a dedicated server and have this process connect to it as a client.</param>
    public void HostMultiplayerGameAsClient(
        string saveFileName, int? port = null, bool createDedicatedServerProcess = false)
    {
        Game game = _gamePackedScene.Instantiate<Game>();
        game.SetName("Game");
        _mainSceneContainer.ChangeStoredNode(game);

        if (createDedicatedServerProcess)
        {
            game.Init(new HostDedicatedServerAndConnectGameStarter(
                saveFileName: saveFileName, 
                port: port, 
                showWindow: true));
        }
        else
        {
            game.Init(new HostMultiplayerGameStarter(saveFileName: saveFileName, port: port));
        }
    }
    
    /// <summary>
    /// Start a new server. Use in the dedicated server process.
    /// </summary>
    /// <param name="saveFileName">Name of the save file in the folder with saves, required non-null</param>
    /// <param name="port">Port number on which the server will listen to.</param>
    /// <param name="adminUid">This user can manage the server</param>
    /// <param name="parentPid">If this process is a dedicated server created from a client,
    /// use the PID of the client process.</param>
    /// <param name="serverHud">Show the ServerHud: false for a server without a window.</param>
    public void HostMultiplayerGameAsDedicatedServer(
        string saveFileName,
        int? port = null,
        string adminUid = null,
        int? parentPid = null,
        bool serverHud = true)
    {
        Game game = _gamePackedScene.Instantiate<Game>();
        game.SetName("Game");
        _mainSceneContainer.ChangeStoredNode(game);

        game.Init(new DedicatedServerGameStarter(
            saveFileName: saveFileName, 
            port: port, 
            adminUid: adminUid, 
            parentPid: parentPid,
            serverHud: serverHud));
        Services.LoadingScreen.Clear();
    }

    public bool MainSceneIsMainMenu()
    {
        return _mainSceneContainer.GetCurrentStoredNode<Node>() is MainMenu;
    }

    public bool MainSceneIsGame()
    {
        return _mainSceneContainer.GetCurrentStoredNode<Node>() is Game;
    }
    
    public void Shutdown()
    {
        Callable.From(() =>
        {
            LeaveGame(() => _mainSceneContainer.GetTree().Quit());
        }).CallDeferred();
    }

    // Freeing the Game closes its connection, and a child server stops once its admin has left: the next scene
    // waits for that, so the server saves and no second one starts beside it
    private void LeaveGame(Action then)
    {
        _mainSceneContainer.ClearStoredNode();
        Services.Process.WaitForDedicatedServerExit(then);
    }

    private MainMenu ShowMainMenu()
    {
        MainMenu mainMenu = _mainMenuPackedScene.Instantiate<MainMenu>();
        _mainSceneContainer.ChangeStoredNode(mainMenu);
        Services.LoadingScreen.Clear();
        return mainMenu;
    }
}