using NeonWarfare.SmokeTests.Infrastructure;
using Xunit;
using static NeonWarfare.SmokeTests.Scenarios.Launches;

namespace NeonWarfare.SmokeTests.Scenarios;

public sealed class ClientStartsToMenuTests
{
    /// <summary>
    /// The plain client launch: the menu comes up and nothing else happens.
    /// </summary>
    [Fact]
    public void Client_StartsToMenu()
    {
        SmokeRun.Run(new GameLaunch("client", [], [ClientStarted]));
    }
}
