using GdUnit4;
using NeonWarfare.GameTests.KludgeBox.Persistence.Fixtures;
using NeonWarfare.Scripts;
using NeonWarfare.Scripts.GlobalServices;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Scripts.GlobalServices;

// The real disk, in a folder of its own under user:// of the test project. A failure is forced by a directory where
// the service wants to put a file
[TestSuite]
public class SaveLoadServiceTests
{
    private static readonly SaveFileName FileName = new("save");

    private static readonly byte[] Old = [1, 2, 3];
    private static readonly byte[] New = [4, 5, 6, 7];

    private TestUserFolder _folder = null!;
    private SaveLoadService _service = null!;

    [BeforeTest]
    public void SetUp()
    {
        _folder = new TestUserFolder();
        _service = new SaveLoadService(_folder.FileName("saves"));
    }

    [AfterTest]
    public void TearDown()
    {
        _folder.Dispose();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void SaveToDisk_CreatesTheFolder_ReplacesTheFile()
    {
        _service.SaveToDisk(Old, FileName);

        _service.SaveToDisk(New, FileName);

        AssertThat(_service.LoadFromDisk(FileName)).IsEqual(New);
        AssertThat(_service.GetAllSaveFiles().Select(file => file.FileName)).ContainsExactly(FileName);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void SaveToDisk_WriteFails_ThrowsSaveException_ThePreviousFileStays()
    {
        _service.SaveToDisk(Old, FileName);
        Directory.CreateDirectory($"{_service.GetFullPath(FileName)}.{Environment.ProcessId}.tmp");

        // Caught by hand: AssertThrown reports the innermost exception
        Exception? thrown = null;
        try
        {
            _service.SaveToDisk(New, FileName);
        }
        catch (Exception e)
        {
            thrown = e;
        }

        AssertThat(thrown).IsInstanceOf<SaveLoadService.SaveException>();

        AssertThat(_service.LoadFromDisk(FileName)).IsEqual(Old);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void LoadFromDisk_ReadFails_ThrowsLoadException()
    {
        Directory.CreateDirectory(_service.GetFullPath(FileName));

        // Caught by hand: AssertThrown reports the innermost exception
        Exception? thrown = null;
        try
        {
            _service.LoadFromDisk(FileName);
        }
        catch (Exception e)
        {
            thrown = e;
        }

        AssertThat(thrown).IsInstanceOf<SaveLoadService.LoadException>();
    }
}
