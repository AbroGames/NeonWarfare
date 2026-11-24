using System;
using NeonWarfare.Scenes.Worlds.Ports;
using NeonWarfare.Scripts.Content.LoadingScreen;
using NeonWarfare.Scripts.GlobalServices.ResumableGame;

namespace NeonWarfare.Scenes.Game.Starters;

/// <summary>
/// A host without ENet: its only peer is its own.
/// </summary>
public class SingleplayerGameStarter(string saveFileName) : BaseGameStarter
{
    public override void Start(Game game)
    {
        Services.LoadingScreen.SetLoadingScreen(LoadingScreenTypes.Type.Loading);

        ResumableGame lastGame = ResumableGame.GetSingleplayer(saveFileName);
        SetLastGame(lastGame);
        FollowLocalPlayer(game);

        try
        {
            game.AddHostWorld(SaveFilesUpdatingLastGame(lastGame), ReadLocalPlayer(), LoadServerOrigin(saveFileName));
        }
        catch (Exception e) when (IsLoadError(e))
        {
            GoToMenuAndShowError(LogLoadError(e, saveFileName));
        }
    }
}
