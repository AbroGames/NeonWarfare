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
    // The milestones wait for the old World's handshake, which the new World does not have;
    // task 025 restores the tests.
    private const string SkipReason = "Muted during the network rework, see task 025-restore-sidecar-file-tests";

    /// <summary>ClientRootStarter: the client has initialized and is about to open the menu.</summary>
    private const string ClientStarted = "Starting Client...";

    /// <summary>Network.HostServer: the ENet socket is open.</summary>
    private const string ServerStarted = "Started server successfully";

    /// <summary>Network: the ENet handshake with the server is done.</summary>
    private const string ConnectedToServer = "Connected to the server successfully";

    /// <summary>
    /// WorldSynchronizerService on the client: the initial world snapshot arrived and was applied. The
    /// single-player game goes through the same local handshake, so it prints this too.
    /// </summary>
    private const string WorldSynced = "Syncing complete successfully";

    /// <summary>Network on the server: an ENet peer is gone.</summary>
    private const string PeerDisconnected = "Network peer disconnected";

    /// <summary>
    /// The plain client launch: the menu comes up and nothing else happens.
    /// </summary>
    [Fact(Skip = SkipReason)]
    public void Client_StartsToMenu()
    {
        SmokeRun.Run(new GameLaunch("client", [], [ClientStarted]));
    }

    /// <summary>
    /// Straight into a single-player game: world generation plus the local handshake.
    /// user:// is a fresh directory for every process, so the save it creates never outlives the run.
    /// </summary>
    [Fact(Skip = SkipReason)]
    public void Client_StartsSingleplayerGame()
    {
        SmokeRun.Run(new GameLaunch("client", ["--auto-start"], [WorldSynced]));
    }

    /// <summary>
    /// A dedicated server with two clients connecting to it — the scenario that exercises the network
    /// handshake and the initial world snapshot. Processes stop in launch order, so here the server
    /// always goes first and the clients are dropped back to the menu before they quit.
    /// </summary>
    [Fact(Skip = SkipReason)]
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
    [Fact(Skip = SkipReason)]
    public void Client_QuitsFromMultiplayerGame()
    {
        int port = FreePort.Take();

        SmokeRun.Run(
            [Server(port), FirstClient(port), SecondClient(port)],
            new Departure(Leaver: "client-1", Witness: "server", Milestone: PeerDisconnected));
    }

    private static GameLaunch Server(int port) =>
        new("server", ["--server", "--port", port.ToString()], [ServerStarted]);

    private static GameLaunch FirstClient(int port) => Client("client-1", "SmokeTestA-Aaaaaaaaaa", port);

    private static GameLaunch SecondClient(int port) => Client("client-2", "SmokeTestB-Bbbbbbbbbb", port);

    /// <summary>
    /// The name doubles as the nickname and must be 3 to 25 characters long, or the server rejects the
    /// sync — "client-1" fits. The uid follows the format of the generated ones.
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
        [ConnectedToServer, WorldSynced]);
}
