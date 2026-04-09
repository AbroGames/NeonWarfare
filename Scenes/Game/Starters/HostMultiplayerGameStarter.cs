using Godot;
using Humanizer;
using KludgeBox.Godot.Nodes.Process;
using NeonWarfare.Scripts.Content.LoadingScreen;

namespace NeonWarfare.Scenes.Game.Starters;

public class HostMultiplayerGameStarter(
    string saveFileName,
    int? port,
    string adminNickname,
    int? parentPid,
    bool serverHudRender,
    bool worldRender,
    bool mustSetLastGame,
    bool startedAsDedicated
    ) : BaseGameStarter
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

        if (serverHudRender)
        {
            game.AddServerHud();
        }
        if (!worldRender)
        {
            world.SetVisible(false);
        }
        if (mustSetLastGame)
        {
            var lastGame = ResumableGame.GetCreateServer(saveFileName, port ?? DefaultPort, startedAsDedicated);
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