using GdUnit4;
using NeonWarfare.GameTests.World.Fixtures;
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

    // The file is written, so the save must not be reported as failed
    [TestCase]
    [RequireGodotRuntime]
    public void SaveToDisk_LastGameFails_DoesNotThrow()
    {
        var disk = new RecordingSaveFiles();
        var files = new LastGameUpdatingSaveFiles(disk, _ => throw new InvalidOperationException("last game"));

        files.SaveToDisk(Data, "save");

        AssertThat(disk.Files.Select(file => file.FileName)).ContainsExactly("save");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void IsAutoSaveEnabled_IsTheDisksSetting()
    {
        var disk = new RecordingSaveFiles { AutoSaveEnabled = false };

        AssertThat(new LastGameUpdatingSaveFiles(disk, _ => { }).IsAutoSaveEnabled()).IsFalse();
    }
}
