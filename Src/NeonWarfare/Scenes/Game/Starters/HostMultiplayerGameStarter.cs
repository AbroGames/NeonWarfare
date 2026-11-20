using Godot;
using Humanizer;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Ports;

namespace NeonWarfare.Scenes.Game.Starters;

/// <summary>
/// A server hosted from inside the client: the host plays in it too.
/// </summary>
public class HostMultiplayerGameStarter(
    string saveFileName,
    int? port
    ) : BaseHostGameStarter(saveFileName, port, mustSetLastGame: true, isDedicated: false), IServerOwner
{
    // TODO Localization debt: player-visible text must go through Tr(KEY), see Docs/Localization.md
    private const string HostingFailedMessage = "Failed to start server: {0}";

    private LocalPlayer _localPlayer;

    public override void Init(Game game)
    {
        _localPlayer = ReadLocalPlayer();
        GoToMenuOnJoinRejected(game);
        ClearLoadingScreenOnJoined(game);
        base.Init(game);
    }

    protected override World AddWorld(Game game, WorldOrigin origin, ISaveFiles saveFiles) =>
        game.AddWorld(new WorldSetup.Host(saveFiles, _localPlayer, this), origin, Game.Screen.Hud);

    // The admin is this process's own player, which leaves only with the process: there is nothing to stop
    public void AdminLeft() { }

    protected override void OnServerOpened(Game game)
    {
        game.SendJoinRequest(_localPlayer);
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
