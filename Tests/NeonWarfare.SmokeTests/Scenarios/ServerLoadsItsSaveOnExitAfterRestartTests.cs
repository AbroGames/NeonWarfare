using NeonWarfare.SmokeTests.Infrastructure;
using Xunit;
using static NeonWarfare.SmokeTests.Scenarios.Launches;

namespace NeonWarfare.SmokeTests.Scenarios;

public sealed class ServerLoadsItsSaveOnExitAfterRestartTests
{
    private const string SaveFile = "smoke-server-world";

    /// <summary>
    /// The whole lifecycle of a server world across processes. A client hosts it in a child dedicated server; the
    /// client, stopped by SIGTERM, waits for the child, which saves on exit once its admin has left. Then a dedicated
    /// server process loads that save, a client joins it over the network, and the server saves on exit on SIGTERM.
    /// The child saved into the user:// of its client, so the second server is named after the client to get the
    /// same one.
    /// </summary>
    [Fact]
    public void Server_LoadsItsSaveOnExitAfterRestart()
    {
        using UserDataDirectories userData = new();
        int port = FreePort.Take();
        GameLaunch host = new(
            "host",
            [
                "--auto-host-dedicated", "--auto-host-dedicated-headless",
                "--auto-host-port", port.ToString(),
                "--auto-host-savefile", SaveFile,
            ],
            [ServerOpened, ConnectedToServer, EnteredWorld])
        {
            ExitMilestones = [SavedOnExit(SaveFile), ChildServerExited],
        };

        SmokeRun.Run([host], userData: userData);
        SmokeRun.Run(
            [
                Server(port, SaveFile) with
                {
                    Name = host.Name,
                    Milestones = [LoadedSave(SaveFile), ServerOpened],
                    ExitMilestones = [SavedOnExit(SaveFile)],
                },
                FirstClient(port),
            ],
            userData: userData);
    }
}
