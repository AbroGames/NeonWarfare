using GdUnit4;
using NeonWarfare.GameTests.Worlds.Fixtures;
using NeonWarfare.Scenes.Game.Starters;
using NeonWarfare.Scripts;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Game.Starters;

// In the engine: outside it the logging of a failed last game update crashes the test host
[TestSuite]
public class LastGameUpdatingSaveFilesTests
{
    private static readonly byte[] Data = [1, 2, 3];
    private static readonly SaveFileName FileName = new("save");

    [TestCase]
    [RequireGodotRuntime]
    public void SaveToDisk_Written_TheLastGameGetsItsName()
    {
        var disk = new RecordingSaveFiles();
        List<SaveFileName> lastGame = [];

        new LastGameUpdatingSaveFiles(disk, lastGame.Add).SaveToDisk(Data, FileName);

        AssertThat(disk.Files.Select(file => file.FileName)).ContainsExactly(FileName);
        AssertThat(lastGame).ContainsExactly(FileName);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void SaveToDisk_DiskFails_Throws_TheLastGameStays()
    {
        var disk = new RecordingSaveFiles { Failing = true };
        List<SaveFileName> lastGame = [];

        AssertThrown(() => new LastGameUpdatingSaveFiles(disk, lastGame.Add).SaveToDisk(Data, FileName))
            .IsInstanceOf<IOException>();
        AssertThat(lastGame).IsEmpty();
    }
}
