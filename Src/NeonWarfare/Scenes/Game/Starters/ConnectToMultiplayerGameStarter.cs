using NeonWarfare.Scripts.GlobalServices.ResumableGame;

namespace NeonWarfare.Scenes.Game.Starters;

/// <summary>
/// An ENet client connecting to a server in another process or machine.
/// </summary>
public class ConnectToMultiplayerGameStarter(string host, int? port) : BaseGameStarter
{
    public override void Start(Game game)
    {
        SetLastGame(ResumableGame.GetConnectToServer(host ?? DefaultHost, port ?? DefaultPort));
        ConnectAndFollow(game, ReadLocalPlayer(), host ?? DefaultHost, port ?? DefaultPort);
    }
}
