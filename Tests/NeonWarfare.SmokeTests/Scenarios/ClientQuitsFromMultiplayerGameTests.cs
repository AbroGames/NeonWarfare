using NeonWarfare.SmokeTests.Infrastructure;
using Xunit;
using static NeonWarfare.SmokeTests.Scenarios.Launches;

namespace NeonWarfare.SmokeTests.Scenarios;

public sealed class ClientQuitsFromMultiplayerGameTests
{
    private const string SaveFile = "smoke-hosted-world";

    /// <summary>
    /// A client quits right from the game while the host and the other client keep playing. This is the only
    /// path where a client tears down a live world on exit, and where the server sees a player leave. The server
    /// is hosted inside a client, the only scenario where remote clients join a host.
    /// </summary>
    [Fact]
    public void Client_QuitsFromMultiplayerGame()
    {
        int port = FreePort.Take();
        GameLaunch host = new(
            "host",
            ["--auto-host", "--auto-host-port", port.ToString(), "--auto-host-savefile", SaveFile],
            [ServerOpened]);

        SmokeRun.Run(
            [host, FirstClient(port), SecondClient(port)],
            new Departure(
                Leaver: FirstClientName, Witness: host.Name,
                Milestone: $"{PlayerLeft}{FirstClientName} ({FirstClientUid})"));
    }
}
