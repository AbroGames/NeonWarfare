using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Ports;
using NeonWarfare.Scripts.Content.LoadingScreen;
using NeonWarfare.Scripts.GlobalServices.ResumableGame;

namespace NeonWarfare.Scenes.Game.Starters;

/// <summary>
/// A host without ENet: its only peer is its own.
/// </summary>
public class SingleplayerGameStarter(
    string saveFileName
    ) : BaseGameStarter, IServerOwner, ILocalPlayerOwner
{
    private Game _game;

    public override void Init(Game game)
    {
        _game = game;
        Services.LoadingScreen.SetLoadingScreen(LoadingScreenTypes.Type.Loading);

        ResumableGame lastGame = ResumableGame.GetSingleplayer(saveFileName);
        SetLastGame(lastGame);
        ISaveFiles saveFiles = SaveFilesUpdatingLastGame(lastGame);
        LocalPlayer localPlayer = ReadLocalPlayer();
        World world = AddServerWorld(
            saveFileName,
            origin => game.AddHostWorld(new WorldSetup.Host(saveFiles, localPlayer, this, this), origin),
            out string loadError);
        if (world == null)
        {
            GoToMenuAndShowError(loadError);
        }
    }

    // The admin is this process's own player, which leaves only with the process: there is nothing to stop
    public void AdminLeft() { }

    public void Joined() => ShowHudOnJoined(_game);

    public void JoinRejected(JoinRejectReason reason) => GoToMenuOnJoinRejected(_game, reason);
}
