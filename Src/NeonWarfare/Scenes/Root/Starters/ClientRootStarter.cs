using KludgeBox.Logging;
using NeonWarfare.Scripts.Content.CmdArgs;
using NeonWarfare.Scripts.Content.LoadingScreen;
using Serilog;

namespace NeonWarfare.Scenes.Root.Starters;

public class ClientRootStarter : BaseRootStarter
{

	private ClientArgs _clientArgs;
	private readonly ILogger _log = LogFactory.GetForStatic<ClientRootStarter>();
	
    public override void Init(RootData rootData)
    {
	    base.Init(rootData);
        _log.Information("Initializing Client...");
        
        _clientArgs = ClientArgs.GetFromCmd(CmdArgsService);
        
        Services.SaveLoad.Init(false);
        Services.AutoScaling.Init(rootData.SceneTree, Consts.AutoScalingSettings);
        Services.Process.Init(rootData.SceneTree);
        Services.LastGame.Init();
        Services.KnownServers.Init();
        
        Services.GameSettings.Init();
        if (_clientArgs.Nick != null) Services.GameSettings.SetNickTemporarily(_clientArgs.Nick);
        if (_clientArgs.Uid != null) Services.GameSettings.SetUidTemporarily(_clientArgs.Uid);
        
        // Set locale only after loading GameSettings
        Services.I18N.SetCurrentLocale(Services.GameSettings.GetSettings().Locale);
        
        // Activate loading screen after setting up locale
        Services.LoadingScreen.SetLoadingScreen(LoadingScreenTypes.Type.Loading);
    }

    public override void Start(RootData rootData)
    {
	    base.Start(rootData);
        _log.Information("Starting Client...");


        if (_clientArgs.AutoStart)
        {
	        Services.MainScene.StartSingleplayerGame(
		        _clientArgs.AutoStartSaveFileName ?? Services.SaveLoad.GenNewSaveFileName());
        } 
        else if (_clientArgs.AutoConnect)
        {
	        Services.MainScene.ConnectToMultiplayerGame(_clientArgs.AutoConnectIp, _clientArgs.AutoConnectPort);
        }
        else
        {
	        Services.MainScene.StartMainMenu();
        }
    }
}