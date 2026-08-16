using Godot;
using GodotBox;
using NeonWarfare.Scenes.Game;
using NeonWarfare.Scenes.Game.Starters;
using NeonWarfare.Scenes.Screen.Menu.MainMenu;

namespace NeonWarfare.Scripts.Service;

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
    
    public void StartMainMenu()
    {
        var mainMenu = _mainMenuPackedScene.Instantiate();
        _mainSceneContainer.ChangeStoredNode(mainMenu);
    }

    public void StartMainMenu(string message)
    {
        StartMainMenu();
        var mainMenu = _mainSceneContainer.GetCurrentStoredNode<MainMenu>();
        
        // We must call this section after adding MainMenu to tree, because otherwise we can't
        // access mainMenu.PagesProvider property
        mainMenu.PushPage(mainMenu.PagesProvider.PrepareMessagePage(message));
    }
    
    public void StartSingleplayerGame(string saveFileName)
    {
        Game game = _gamePackedScene.Instantiate<Game>();
        game.SetName("Game");
        _mainSceneContainer.ChangeStoredNode(game);
        
        game.Init(new SingleplayerGameStarter(saveFileName));
    }
    
    public void ConnectToMultiplayerGame(string host = null, int? port = null)
    {
        Game game = _gamePackedScene.Instantiate<Game>();
        game.SetName("Game");
        _mainSceneContainer.ChangeStoredNode(game);
        
        game.Init(new ConnectToMultiplayerGameStarter(host, port, true));
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

        string adminUid = Services.GameSettings.GetSettings().PlayerUid;
        
        if (createDedicatedServerProcess)
        {
            game.Init(new HostDedicatedServerAndConnectGameStarter(saveFileName, port, adminUid, true));
        }
        else
        {
            game.Init(new HostMultiplayerGameStarter(saveFileName, port, adminUid, null,false, true, true, false));
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
    /// <param name="noHudRender">Don't show ServerHud. Could be used in a dedicated server
    /// to show only the world game scene.</param>
    /// <param name="worldRender">Show the game scene behind gui. Could be disabled
    /// in a dedicated server to show only the ServerHud.</param>
    public void HostMultiplayerGameAsDedicatedServer(
        string saveFileName,
        int? port = null,
        string adminUid = null,
        int? parentPid = null,
        bool noHudRender = false,
        bool worldRender = false)
    {
        Game game = _gamePackedScene.Instantiate<Game>();
        game.SetName("Game");
        _mainSceneContainer.ChangeStoredNode(game);
        
        // Don't set LastGame in dedicated server started from console
        bool mustSetLastGame = parentPid.HasValue;
        
        game.Init(new HostMultiplayerGameStarter(
            saveFileName, port, adminUid, parentPid, !noHudRender, worldRender, mustSetLastGame, true));
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
        Callable.From(() => { 
            _mainSceneContainer.GetTree().Quit();
        }).CallDeferred();
    }
}