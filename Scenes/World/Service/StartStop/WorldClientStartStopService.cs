using System;
using Godot;
using NeonWarfare.Scenes.World.Service.Performance;
using NeonWarfare.Scripts.Content.LoadingScreen;
using NeonWarfare.Scripts.Service.Settings;
using Humanizer;
using KludgeBox.DI.Requests.LoggerInjection;
using KludgeBox.DI.Requests.SceneServiceInjection;
using Serilog;

namespace NeonWarfare.Scenes.World.Service.StartStop;

public partial class WorldClientStartStopService : Node
{
    
    private const string SyncRejectedMessage = "Synchronization with the server was rejected: {0}";
    
    [SceneService] private WorldSynchronizerService _synchronizerService;
    [SceneService] private WorldPerformanceService _performanceService;
    [Logger] private ILogger _log;

    public override void _Ready()
    {
        Di.Process(this);
    }

    public void StartSyncWithServer(Action<string> goToMenuAndShowErrorAction)
    {
        if (!Net.IsClient()) throw new InvalidOperationException("Can only be executed on the client");
        _log.Information("World starting...");
        
        _synchronizerService.SyncStartedOnClientEvent += OnSyncStarted;
        _synchronizerService.SyncEndedOnClientEvent += OnSyncEnded;
        _synchronizerService.SyncRejectOnClientEvent += 
            errorMessage => goToMenuAndShowErrorAction.Invoke(SyncRejectedMessage.FormatWith(errorMessage));
        
        PlayerSettings playerSettings = Services.PlayerSettings.GetPlayerSettings();
        _synchronizerService.StartSyncOnClient(playerSettings.Nick, playerSettings.Color);
    }
    
    private void OnSyncStarted()
    {
        Services.LoadingScreen.SetLoadingScreen(LoadingScreenTypes.Type.Loading);
    }

    private void OnSyncEnded()
    {
        _performanceService.Ping.Start();
        Services.LoadingScreen.Clear();
    }
}