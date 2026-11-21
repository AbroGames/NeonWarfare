using Godot;
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
        if (mustSetLastGame)
        {
            SetLastGame(ResumableGame.GetCreateServer(saveFileName, port ?? DefaultPort, isDedicated));
        }

        Error error = network.HostServer(port ?? DefaultPort);
        if (error != Error.Ok)
        {
            OnHostingFailed(error);
            return;
        }

        //TODO 022b load the save; 028 adminUid
        AddWorld(game);
        network.OpenServer();
        OnServerOpened(game);
    }

    protected abstract void AddWorld(Game game);

    protected virtual void OnServerOpened(Game game) { }

    // Network has already logged the error
    protected virtual void OnHostingFailed(Error error) { }
}
