using NeonWarfare.SmokeTests.Infrastructure;
using Xunit;
using static NeonWarfare.SmokeTests.Scenarios.Launches;

namespace NeonWarfare.SmokeTests.Scenarios;

public sealed class ServerAcceptsTwoClientsTests
{
    private const string SaveFile = "smoke-two-clients-world";

    /// <summary>
    /// A dedicated server with two clients connecting to it — the scenario that exercises the network
    /// handshake and the initial world snapshot. Processes stop in launch order, so here the server
    /// always goes first and the clients are dropped back to the menu before they quit.
    /// </summary>
    [Fact]
    public void Server_AcceptsTwoClients()
    {
        int port = FreePort.Take();

        SmokeRun.Run(Server(port, SaveFile), FirstClient(port), SecondClient(port));
    }
}
