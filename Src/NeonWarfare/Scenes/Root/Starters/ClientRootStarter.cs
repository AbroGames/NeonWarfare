using KludgeBox.Logging;
using NeonWarfare.Scripts.Content.CmdArgs;
using NeonWarfare.Scripts.Content.LoadingScreen;
using Serilog;

namespace NeonWarfare.Scenes.Root.Starters;

public class ClientRootStarter(RootData rootData) : BaseRootStarter(rootData)
{

    private ClientArgs _clientArgs;
    private readonly ILogger _log = LogFactory.GetForStatic<ClientRootStarter>();
    
    public override void Init()
    {
        base.Init();
        _log.Information("Initializing Client...");
        
        _clientArgs = ClientArgs.GetFromCmd(CmdArgsService);
        
        Services.SaveLoad.Init(false);
        Services.AutoScaling.Init(RootData.SceneTree, Consts.AutoScalingSettings);
        Services.Process.Init(RootData.SceneTree);
        Services.LastGame.Init();
        Services.KnownServers.Init();
        
        Services.GameSettings.Init();
        if (_clientArgs.Nick != null) Services.GameSettings.SetNickTemporarily(_clientArgs.Nick);
        if (_clientArgs.Uid != null) Services.GameSettings.SetUidTemporarily(_clientArgs.Uid);
        
        // The locale is a setting, so it is set only after the settings are loaded.
        Services.I18N.SetCurrentLocale(Services.GameSettings.GetSettings().Locale);
        
        // The loading screen comes after the locale, so its text is already translated.
        Services.LoadingScreen.SetLoadingScreen(LoadingScreenTypes.Type.Loading);
    }

    public override void Start()
    {
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