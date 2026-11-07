using Godot;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Saves;
using NeonWarfare.Scripts.Content.LoadingScreen;
using NeonWarfare.Scripts.GlobalServices.ResumableGame;

namespace NeonWarfare.Scenes.Game.Starters;

/// <summary>
/// An ENet server in this same process.
/// </summary>
public abstract class BaseHostGameStarter(
    string saveFileName,
    int? port,
    string adminUid,
    bool mustSetLastGame,
    bool isDedicated
    ) : BaseGameStarter
{
    public override void Init(Game game)
    {
        Services.LoadingScreen.SetLoadingScreen(LoadingScreenTypes.Type.Loading);

        Network.Network network = game.AddNetwork();
        ResumableGame lastGame = ResumableGame.GetCreateServer(saveFileName, port ?? DefaultPort, isDedicated);
        if (mustSetLastGame)
        {
            SetLastGame(lastGame);
        }

        Error error = network.HostServer(port ?? DefaultPort);
        if (error != Error.Ok)
        {
            OnHostingFailed(error);
            return;
        }

        //TODO 028 adminUid
        ISaveFiles saveFiles = mustSetLastGame ? SaveFilesUpdatingLastGame(lastGame) : Services.SaveLoad;
        World.World world = AddServerWorld(
            saveFileName, origin => AddWorld(game, origin, saveFiles), out string loadError);
        if (world == null)
        {
            OnLoadFailed(loadError);
            return;
        }

        network.OpenServer();
        OnServerOpened(game);
    }

    protected abstract World.World AddWorld(Game game, WorldOrigin origin, ISaveFiles saveFiles);

    protected virtual void OnServerOpened(Game game) { }

    // Network has already logged the error
    protected virtual void OnHostingFailed(Error error) { }

    // The server is not opened: nobody has connected yet
    protected abstract void OnLoadFailed(string message);
}
