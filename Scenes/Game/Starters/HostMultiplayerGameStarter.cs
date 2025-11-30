using Godot;
using KludgeBox.Godot.Nodes.Process;
using NeonWarfare.Scripts.Content.LoadingScreen;
using NeonWarfare.Scripts.Service.Settings;

namespace NeonWarfare.Scenes.Game.Starters;

public class HostMultiplayerGameStarter(int? port = null, string saveFileName = null, string adminNickname = null, int? parentPid = null) : BaseGameStarter
{
    public override void Init(Game game)
    {
        base.Init(game);
        Services.LoadingScreen.SetLoadingScreen(LoadingScreenTypes.Type.Loading);

        if (parentPid.HasValue)
        {
            ProcessDeadChecker clientDeadChecker = new ProcessDeadChecker(
                parentPid.Value, 
                () => Services.MainScene.Shutdown(),
                pid => $"Parent process {pid} is dead. Shutdown server.");
            game.AddChild(clientDeadChecker);
        }
        
        PlayerSettings playerSettings = Services.PlayerSettings.GetPlayerSettings();
        World.World world = game.AddWorld();
        Synchronizer synchronizer = game.AddSynchronizer(playerSettings);
        Net.DoClient(() => game.AddHud());
        Network.Network network = game.AddNetwork();
        
        Error error = network.HostServer(port ?? DefaultPort, true);
        if (error != Error.Ok)
        {
            Net.DoClient(HostFailedEventOnClient);
            return;
        }

        if (saveFileName == null)
        {
            world.StartStopService.StartNewGame(adminNickname);
        }
        else
        {
            world.StartStopService.LoadGame(saveFileName, adminNickname);
        }
        network.OpenServer();
        Net.DoClient(synchronizer.StartSyncOnClient);
    }
    
    private void HostFailedEventOnClient()
    {
        Services.MainScene.StartMainMenu();
        //TODO Show error in menu (it is client). Log already has error.
        Services.LoadingScreen.Clear();
    }
}