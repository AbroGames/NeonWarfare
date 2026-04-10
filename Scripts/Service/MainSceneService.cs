using Godot;
using NeonWarfare.Scenes.Game;
using NeonWarfare.Scenes.Game.Starters;
using NeonWarfare.Scenes.KludgeBox;
using NeonWarfare.Scenes.Screen.NewMenu.MainMenu;

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
        
        // We must call this section after adding MainMenu to tree, because otherwise we can't access mainMenu.PagesProvider property
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
    /// Start new server and connect to them. Use in client process.
    /// </summary>
    /// <param name="saveFileName">Name of the save file in folder with saves. Null for start new game.</param>
    /// <param name="port">Port number on which the server will listen.</param>
    /// <param name="createDedicatedServerProcess">If true, create a new OS process running a dedicated server, and have this process connect to it as a client.</param>
    public void HostMultiplayerGameAsClient(string saveFileName, int? port = null, bool createDedicatedServerProcess = false)
    {
        Game game = _gamePackedScene.Instantiate<Game>();
        game.SetName("Game");
        _mainSceneContainer.ChangeStoredNode(game);

        string adminNickname = Services.GameSettings.GetSettings().PlayerNick;
        
        if (createDedicatedServerProcess)
        {
            game.Init(new HostDedicatedServerAndConnectGameStarter(saveFileName, port, adminNickname, true));
        }
        else
        {
            game.Init(new HostMultiplayerGameStarter(saveFileName, port, adminNickname, null,false, true, true, false));
        }
    }
    
    /// <summary>
    /// Start new server. Use in dedicated server process.
    /// </summary>
    /// <param name="saveFileName">Name of the save file in folder with saves. Null for start new game.</param>
    /// <param name="port">Port number on which the server will listen.</param>
    /// <param name="adminNickname">This user can manage the server</param>
    /// <param name="parentPid">If this process is a dedicated server created from a client, use the PID of the client process.</param>
    /// <param name="noHudRender">Don't show ServerHud. Could be use in dedicated server for show only world game scene.</param>
    /// <param name="worldRender">Show game scene behind gui. Could be disabled in dedicated server for show only ServerHud.</param>
    public void HostMultiplayerGameAsDedicatedServer(string saveFileName, int? port = null, string adminNickname = null, int? parentPid = null, bool noHudRender = false, bool worldRender = false)
    {
        Game game = _gamePackedScene.Instantiate<Game>();
        game.SetName("Game");
        _mainSceneContainer.ChangeStoredNode(game);
        
        // Don't set LastGame in dedicated server started from console
        bool mustSetLastGame = parentPid.HasValue;
        
        game.Init(new HostMultiplayerGameStarter(saveFileName, port, adminNickname, parentPid, !noHudRender, worldRender, mustSetLastGame, true));
        Services.LoadingScreen.Clear();
    }
    
    public void StartResumableGame(ResumableGame game)
    {
        switch (game.Type)
        { 
            case ResumableGame.ResumableType.RunSingleplayer: 
                StartSingleplayerGame(game.SaveName);
                break;
            case ResumableGame.ResumableType.ConnectToServer: 
                ConnectToMultiplayerGame(game.Host, game.Port);
                break;
            case ResumableGame.ResumableType.CreateServer: 
                HostMultiplayerGameAsClient(game.SaveName, game.Port, game.IsDedicated!.Value);
                break;
        }
    }
    
    public void StartLastGame()
    {
        StartResumableGame(Services.GameSettings.GetSettings().LastGame);
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