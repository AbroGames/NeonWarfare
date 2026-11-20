using Godot;
using Humanizer;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Ports;

namespace NeonWarfare.Scenes.Game.Starters;

/// <summary>
/// A server hosted from inside the client: the host plays in it too.
/// </summary>
public class HostMultiplayerGameStarter(
    string saveFileName,
    int? port
    ) : BaseHostGameStarter(saveFileName, port, mustSetLastGame: true, isDedicated: false), IServerOwner,
    ILocalPlayerOwner
{
    // TODO Localization debt: player-visible text must go through Tr(KEY), see Docs/Localization.md
    private const string HostingFailedMessage = "Failed to start server: {0}";

    private Game _game;
    private LocalPlayer _localPlayer;

    public override void Init(Game game)
    {
        _game = game;
        _localPlayer = ReadLocalPlayer();
        base.Init(game);
    }

    // The host's join leaves before the server is opened, so the host is the first to join
    protected override World AddWorld(Game game, WorldOrigin origin, ISaveFiles saveFiles) =>
        game.AddHostWorld(new WorldSetup.Host(saveFiles, _localPlayer, this, this), origin);

    // The admin is this process's own player, which leaves only with the process: there is nothing to stop
    public void AdminLeft() { }

    public void Joined() => ShowHudOnJoined(_game);

    public void JoinRejected(JoinRejectReason reason) => GoToMenuOnJoinRejected(_game, reason);

    protected override void OnHostingFailed(Error error)
    {
        GoToMenuAndShowError(HostingFailedMessage.FormatWith(error));
    }

    protected override void OnLoadFailed(string message)
    {
        GoToMenuAndShowError(message);
    }
}
