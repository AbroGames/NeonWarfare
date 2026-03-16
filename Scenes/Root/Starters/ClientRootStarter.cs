using NeonWarfare.Scripts.Content.CmdArgs;
using NeonWarfare.Scripts.Content.LoadingScreen;
using KludgeBox.DI.Requests.LoggerInjection;
using Serilog;

namespace NeonWarfare.Scenes.Root.Starters;

public class ClientRootStarter : BaseRootStarter
{

	private ClientArgs _clientArgs;
	[Logger] private ILogger _log;
	
    public override void Init(RootData rootData)
    {
	    base.Init(rootData);
        _log.Information("Initializing Client...");
        
        _clientArgs = ClientArgs.GetFromCmd(CmdArgsService);
        
        Services.Net.Init(rootData.SceneTree, false);
        
        Services.PlayerSettings.Init();
        if (_clientArgs.Nick != null)
        {
	        Services.PlayerSettings.SetNickTemporarily(_clientArgs.Nick);
        }
        Services.I18N.SetCurrentLocale(Services.PlayerSettings.GetPlayerSettings().Language);
        
        // Activate loading screen after setting up locale
        Services.LoadingScreen.SetLoadingScreen(LoadingScreenTypes.Type.Loading);
    }

    public override void Start(RootData rootData)
    {
	    base.Start(rootData);
        _log.Information("Starting Client...");


        if (_clientArgs.AutoStart)
        {
	        Services.MainScene.StartSingleplayerGame();
        } 
        else if (_clientArgs.AutoConnect)
        {
	        Services.MainScene.ConnectToMultiplayerGame(_clientArgs.AutoConnectIp, _clientArgs.AutoConnectPort);
        }
        else
        {
	        Services.MainScene.StartMainMenu();
	        Services.LoadingScreen.Clear();
        }
    }
}