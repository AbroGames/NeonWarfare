using GdUnit4;
using Godot;
using NeonWarfare.Scripts;
using NeonWarfare.Scripts.GlobalServices;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Scripts.GlobalServices;

[TestSuite]
public class ProcessServiceTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void StartNewDedicatedServerApplication_PortTaken_ReturnsTheErrorAndStartsNothing()
    {
        var other = new ENetConnection();
        AssertThat(other.CreateHostBound("*", 0)).IsEqual(Error.Ok);
        try
        {
            var service = new ProcessService();

            Error error = service.StartNewDedicatedServerApplication(
                "save", other.GetLocalPort(), PlayerUid.Generate(), showWindow: false);

            AssertThat(error).IsNotEqual(Error.Ok);
            bool exitedAtOnce = false;
            service.WaitForDedicatedServerExit(() => exitedAtOnce = true);
            AssertThat(exitedAtOnce).IsTrue();
        }
        finally
        {
            other.Destroy();
        }
    }
}
