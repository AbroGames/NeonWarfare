using GdUnit4;
using Godot;
using NeonWarfare.Scripts.GlobalServices;
using static GdUnit4.Assertions;
using FileAccess = Godot.FileAccess;

namespace NeonWarfare.GameTests.Scripts.GlobalServices;

// The real disk, in a folder of its own under user:// of the test project. A failure is forced by a directory where
// the service wants to put a file
[TestSuite]
public class SaveLoadServiceTests
{
    private const string FileName = "save";

    private static readonly byte[] Old = [1, 2, 3];
    private static readonly byte[] New = [4, 5, 6, 7];

    private string _dir = null!;
    private SaveLoadService _service = null!;

    [BeforeTest]
    public void SetUp()
    {
        _dir = $"user://save-load-tests-{Guid.NewGuid():N}/";
        DirAccess.MakeDirRecursiveAbsolute(_dir);
        _service = new SaveLoadService(_dir);
    }

    [AfterTest]
    public void TearDown()
    {
        Delete(_dir);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void SaveToDisk_ReplacesTheFile_LeavesNoTemporaryOrBackup()
    {
        _service.SaveToDisk(Old, FileName);

        _service.SaveToDisk(New, FileName);

        AssertThat(_service.LoadFromDisk(FileName)).IsEqual(New);
        AssertThat(DirAccess.GetFilesAt(_dir)).ContainsExactly(FileName + ".bin");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void SaveToDisk_WriteFails_ThePreviousFileStays()
    {
        _service.SaveToDisk(Old, FileName);
        DirAccess.MakeDirAbsolute(Path() + ".tmp");

        AssertThrown(() => _service.SaveToDisk(New, FileName)).IsInstanceOf<SaveLoadService.SaveException>();

        AssertThat(_service.LoadFromDisk(FileName)).IsEqual(Old);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void SaveToDisk_BackupFails_ThePreviousFileStays_TheTemporaryIsRemoved()
    {
        _service.SaveToDisk(Old, FileName);
        DirAccess.MakeDirAbsolute(Path() + ".backup");

        AssertThrown(() => _service.SaveToDisk(New, FileName)).IsInstanceOf<SaveLoadService.SaveException>();

        AssertThat(_service.LoadFromDisk(FileName)).IsEqual(Old);
        AssertThat(DirAccess.GetFilesAt(_dir)).ContainsExactly(FileName + ".bin");
    }

    // A process killed between the two renames leaves only the backup
    [TestCase]
    [RequireGodotRuntime]
    public void Init_RestoresASaveLeftOnlyAsItsBackup()
    {
        Write(Path() + ".backup", Old);

        _service.Init(false);

        AssertThat(_service.GetAllSaveFiles().Select(file => file.FileName)).ContainsExactly(FileName);
        AssertThat(_service.LoadFromDisk(FileName)).IsEqual(Old);
        AssertThat(DirAccess.GetFilesAt(_dir)).ContainsExactly(FileName + ".bin");
    }

    // The file is the newer one: the backup is left by a complete save that could not remove it
    [TestCase]
    [RequireGodotRuntime]
    public void Init_KeepsAFileOverItsBackup()
    {
        Write(Path(), New);
        Write(Path() + ".backup", Old);

        _service.Init(false);

        AssertThat(_service.LoadFromDisk(FileName)).IsEqual(New);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Init_RemovesAnUnfinishedTemporaryFile_KeepsTheSave()
    {
        _service.SaveToDisk(Old, FileName);
        Write(Path() + ".tmp", New);

        _service.Init(false);

        AssertThat(DirAccess.GetFilesAt(_dir)).ContainsExactly(FileName + ".bin");
        AssertThat(_service.LoadFromDisk(FileName)).IsEqual(Old);
    }

    private string Path() => _service.GetFullPath(FileName);

    private static void Write(string path, byte[] data)
    {
        using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        file.StoreBuffer(data);
    }

    private static void Delete(string dir)
    {
        foreach (string child in DirAccess.GetDirectoriesAt(dir))
        {
            Delete(dir + child + "/");
        }
        foreach (string file in DirAccess.GetFilesAt(dir))
        {
            DirAccess.RemoveAbsolute(dir + file);
        }
        DirAccess.RemoveAbsolute(dir);
    }
}
