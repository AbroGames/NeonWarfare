using NeonWarfare.SmokeTests.Infrastructure;
using Xunit;
using static NeonWarfare.SmokeTests.Scenarios.Launches;

namespace NeonWarfare.SmokeTests.Scenarios;

public sealed class ServerShutsDownWhenParentDiesTests
{
    private const string SaveFile = "smoke-orphan-world";

    /// <summary>
    /// A child server whose client died without a word, so no admin ever left: the server notices the parent is
    /// gone, autosaves and exits by itself.
    /// </summary>
    [Fact]
    public void Server_ShutsDownWhenParentDies()
    {
        using StandInProcess parent = new();
        int port = FreePort.Take();
        GameLaunch server = Server(port, SaveFile);

        SmokeRun.Run(
            [
                server with
                {
                    Arguments = [..server.Arguments, "--parent-pid", parent.Pid.ToString()],
                    ExitMilestones = [ParentDied(parent.Pid), Autosaved(SaveFile)],
                },
            ],
            selfExit: new SelfExit(server.Name, parent.Kill));
    }
}
