using GdUnit4;
using NeonWarfare.GameTests.Worlds.Fixtures;
using NeonWarfare.Scenes.Game.Starters;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Game.Starters;

// In the engine: outside it the logging of a failed last game update crashes the test host
[TestSuite]
public class LastGameUpdatingSaveFilesTests
{
    private static readonly byte[] Data = [1, 2, 3];

    [TestCase]
    [RequireGodotRuntime]
    public void SaveToDisk_Written_TheLastGameGetsItsName()
    {
        var disk = new RecordingSaveFiles();
        List<string> lastGame = [];

        new LastGameUpdatingSaveFiles(disk, lastGame.Add).SaveToDisk(Data, "save");

        AssertThat(disk.Files.Select(file => file.FileName)).ContainsExactly("save");
        AssertThat(lastGame).ContainsExactly("save");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void SaveToDisk_DiskFails_Throws_TheLastGameStays()
    {
        var disk = new RecordingSaveFiles { Failing = true };
        List<string> lastGame = [];

        AssertThrown(() => new LastGameUpdatingSaveFiles(disk, lastGame.Add).SaveToDisk(Data, "save"))
            .IsInstanceOf<IOException>();
        AssertThat(lastGame).IsEmpty();
    }
}
