using Godot;
using Humanizer;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Infra.Composition;

namespace NeonWarfare.Scenes.Game.Starters;

/// <summary>
/// A server hosted from inside the client: the host plays in it too.
/// </summary>
public class HostMultiplayerGameStarter(
    string saveFileName,
    int? port,
    string adminUid
    ) : BaseHostGameStarter(saveFileName, port, adminUid, mustSetLastGame: true, isDedicated: false)
{
    // TODO Localization debt: player-visible text must go through Tr(KEY), see Docs/Localization.md
    private const string HostingFailedMessage = "Failed to start server: {0}";

    protected override World.World AddWorld(Game game, WorldOrigin origin) =>
        game.AddWorld(WorldLayer.Host, origin, Game.Screen.Hud);

    protected override void OnServerOpened(Game game)
    {
        SendJoinRequest(game);
        Services.LoadingScreen.Clear();
    }

    protected override void OnHostingFailed(Error error)
    {
        GoToMenuAndShowError(HostingFailedMessage.FormatWith(error));
    }

    protected override void OnLoadFailed(string message)
    {
        GoToMenuAndShowError(message);
    }
}
