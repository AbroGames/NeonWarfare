using NeonWarfare.SmokeTests.Infrastructure;
using Xunit;

namespace NeonWarfare.SmokeTests.Scenarios;

/// <summary>
/// Launches the game the way a player would and checks that it gets where it was sent and says nothing
/// bad on the way.
///
/// These cover what the unit tests structurally cannot: a broken node tree, an injection that comes
/// out null, an exception in _Ready, a client that fails to reach the server. Every process runs
/// headless — see Docs/Smoke-testing.md.
/// </summary>
public sealed class LaunchScenarioTests : IClassFixture<GameBuildFixture>
{
    /// <summary>ClientRootStarter: the client has initialized and is about to open the menu.</summary>
    private const string ClientStarted = "Starting Client...";

    /// <summary>
    /// Network.OpenServer: the server World is loaded and the ENet socket accepts peers. The socket is
    /// open earlier, but it refuses connections until then.
    /// </summary>
    private const string ServerOpened = "Server opened";

    /// <summary>Network: the ENet handshake with the server is done.</summary>
    private const string ConnectedToServer = "Connected to the server successfully";

    /// <summary>
    /// Game: the join snapshot arrived and the client World is built from it.
    /// </summary>
    private const string EnteredWorld = "Entered the world from the join snapshot";

    /// <summary>
    /// PlayerSimulationFacade: the World has joined a player. The single-player game joins its own player
    /// whose uid is generated, so only the prefix is matched.
    /// </summary>
    private const string PlayerJoined = "Player joined: ";

    /// <summary>
    /// PlayerSimulationFacade on the server: the World let the player go, not just ENet the peer. Followed by
    /// <c>nick (uid)</c>.
    /// </summary>
    private const string PlayerLeft = "Player left: ";

    /// <summary>
    /// The plain client launch: the menu comes up and nothing else happens.
    /// </summary>
    [Fact]
    public void Client_StartsToMenu()
    {
        SmokeRun.Run(new GameLaunch("client", [], [ClientStarted]));
    }

    /// <summary>
    /// Straight into a single-player game: world generation plus the local handshake.
    /// user:// is a fresh directory for every process, so the save it creates never outlives the run.
    /// </summary>
    [Fact]
    public void Client_StartsSingleplayerGame()
    {
        SmokeRun.Run(new GameLaunch("client", ["--auto-start"], [PlayerJoined]));
    }

    /// <summary>
    /// A dedicated server with two clients connecting to it — the scenario that exercises the network
    /// handshake and the initial world snapshot. Processes stop in launch order, so here the server
    /// always goes first and the clients are dropped back to the menu before they quit.
    /// </summary>
    [Fact]
    public void Server_AcceptsTwoClients()
    {
        int port = FreePort.Take();

        SmokeRun.Run(Server(port), FirstClient(port), SecondClient(port));
    }

    /// <summary>
    /// The other way round: a client quits right from the game while the server and the other client
    /// keep playing. This is the only path where a client tears down a live world on exit, and where the
    /// server sees a player leave — neither happens in <see cref="Server_AcceptsTwoClients"/>.
    /// </summary>
    [Fact]
    public void Client_QuitsFromMultiplayerGame()
    {
        int port = FreePort.Take();

        SmokeRun.Run(
            [Server(port), FirstClient(port), SecondClient(port)],
            new Departure(
                Leaver: FirstClientName, Witness: "server",
                Milestone: $"{PlayerLeft}{FirstClientName} ({FirstClientUid})"));
    }

    private static GameLaunch Server(int port) =>
        new("server", ["--server", "--port", port.ToString()], [ServerOpened]);

    private const string FirstClientName = "client-1";
    private const string FirstClientUid = "SmokeTestA-Aaaaaaaaaa";
    private const string SecondClientName = "client-2";
    private const string SecondClientUid = "SmokeTestB-Bbbbbbbbbb";

    private static GameLaunch FirstClient(int port) => Client(FirstClientName, FirstClientUid, port);

    private static GameLaunch SecondClient(int port) => Client(SecondClientName, SecondClientUid, port);

    /// <summary>
    /// The name doubles as the nickname and must be 3 to 25 characters long, or the server rejects the
    /// join — "client-1" fits. The uid follows the format of the generated ones.
    /// </summary>
    private static GameLaunch Client(string name, string uid, int port) => new(
        name,
        [
            "--auto-connect",
            "--auto-connect-ip", "127.0.0.1",
            "--auto-connect-port", port.ToString(),
            "--uid", uid,
            "--nick", name,
        ],
        [ConnectedToServer, EnteredWorld]);
}
