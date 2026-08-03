using NeonWarfare.SmokeTests.Infrastructure;
using Xunit;

namespace NeonWarfare.SmokeTests.Scenarios;

/// <summary>
/// Launches the game the way a player would and checks that it says nothing bad while it starts up.
///
/// These cover what the unit tests structurally cannot: a broken node tree, an injection that comes
/// out null, an exception in _Ready, a client that fails to reach the server. Every process runs
/// headless — see Docs/Smoke-testing.md.
/// </summary>
public sealed class LaunchScenarioTests : IClassFixture<GameBuildFixture>
{
    /// <summary>
    /// The plain client launch: the menu comes up and nothing else happens.
    /// </summary>
    [Fact]
    public void Client_StartsToMenu()
    {
        SmokeRun.Run(() => GameProcess.Start("client"));
    }

    /// <summary>
    /// Straight into a single-player game: world generation plus the local handshake.
    /// No save file name is passed, so every run creates a fresh one and runs stay independent.
    /// </summary>
    [Fact]
    public void Client_StartsSingleplayerGame()
    {
        SmokeRun.Run(() => GameProcess.Start("client", "--auto-start"));
    }

    /// <summary>
    /// A dedicated server with two clients connecting to it — the one scenario that exercises the
    /// network handshake and the initial world snapshot.
    /// </summary>
    [Fact]
    public void Server_AcceptsTwoClients()
    {
        int port = FreePort.Take();

        SmokeRun.Run(
            () => GameProcess.Start("server", "--server", "--port", port.ToString()),
            () => Client("client-1", port),
            () => Client("client-2", port));
    }

    private static GameProcess Client(string name, int port) => GameProcess.Start(
        name,
        "--auto-connect",
        "--auto-connect-ip", "127.0.0.1",
        "--auto-connect-port", port.ToString(),
        "--uid", name,
        "--nick", name);
}
