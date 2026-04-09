using Godot;
using Humanizer;
using KludgeBox.Godot.Nodes.Process;
using NeonWarfare.Scripts.Content.LoadingScreen;

namespace NeonWarfare.Scenes.Game.Starters;

public class HostMultiplayerGameStarter(int? port = null, string saveFileName = null, string adminNickname = null, int? parentPid = null, bool? serverHudRender = false, bool? worldRender = true, bool? mustSetLastGame = null, bool? startedAsDedicated = null) : BaseGameStarter
{
    
    private const string HostingFailedMessage = "Failed to start server: {0}";
    
    public override void Init(Game game)
    {
        Services.LoadingScreen.SetLoadingScreen(LoadingScreenTypes.Type.Loading);

        if (parentPid.HasValue)
        {
            ProcessDeadChecker clientDeadChecker = new ProcessDeadChecker(
                parentPid.Value, 
                () => Services.MainScene.Shutdown(),
                pid => $"Parent process {pid} is dead. Shutdown server.");
            game.AddChild(clientDeadChecker);
        }
        
        Network.Network network = game.AddNetwork();
        World.World world = game.AddWorld();
        Net.DoClient(() => game.AddHud());

        if (serverHudRender.HasValue && serverHudRender.Value)
        {
            game.AddServerHud();
        }
        if (worldRender.HasValue && !worldRender.Value)
        {
            world.SetVisible(false);
        }

        
        if (mustSetLastGame.HasValue && mustSetLastGame.Value)
        {
            var lastGame = ResumableGame.GetCreateServer(saveFileName, port ?? 0, startedAsDedicated ?? false);
            SetLastGame(lastGame);
            AddLastGameUpdaterToSaveEvent(world, lastGame);
        }

        Error error = network.HostServer(port ?? DefaultPort, true);
        if (error != Error.Ok)
        {
            Net.DoClient(() => HostingFailedEventOnClient(error));
            return;
        }

        ServerStartWorld(world, saveFileName, adminNickname);
        network.OpenServer();
        Net.DoClient(() => ClientStartWorld(world));
    }

    private void HostingFailedEventOnClient(Error error) => GoToMenuAndShowError(HostingFailedMessage.FormatWith(error));
}