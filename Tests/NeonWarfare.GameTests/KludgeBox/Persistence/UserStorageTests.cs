using GdUnit4;
using KludgeBox.Persistence;
using NeonWarfare.GameTests.KludgeBox.Persistence.Fixtures;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.KludgeBox.Persistence;

// The real disk. A failure is forced by a directory where the storage wants to put a file
[TestSuite]
public class UserStorageTests
{
    private const string Name = "file.json";

    private TestUserFolder _folder = null!;
    private UserStorage _storage = null!;

    [BeforeTest]
    public void SetUp()
    {
        _folder = new TestUserFolder();
        _storage = new UserStorage();
    }

    [AfterTest]
    public void TearDown()
    {
        _folder.Dispose();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void WriteAllText_ReplacesTheFile_LeavesNoTemporary()
    {
        _storage.WriteAllText(_folder.FileName(Name), "old");

        _storage.WriteAllText(_folder.FileName(Name), "new");

        AssertThat(_folder.Read(Name)).IsEqual("new");
        AssertThat(_folder.Entries()).ContainsExactly(Name);
    }

    // The write goes to the temporary file first, so a failure there never touches the file itself
    [TestCase]
    [RequireGodotRuntime]
    public void WriteAllText_WriteFails_ThePreviousFileStays()
    {
        _storage.WriteAllText(_folder.FileName(Name), "old");
        Directory.CreateDirectory(_folder.FullPath(TempName()));

        AssertThrown(() => _storage.WriteAllText(_folder.FileName(Name), "new"));

        AssertThat(_folder.Read(Name)).IsEqual("old");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void WriteAllText_ReplaceFails_LeavesNoTemporary()
    {
        Directory.CreateDirectory(_folder.FullPath(Name));

        AssertThrown(() => _storage.WriteAllText(_folder.FileName(Name), "new"));

        AssertThat(_folder.Entries()).ContainsExactly(Name);
    }

    [TestCase]
    [RequireGodotRuntime]
    public async Task WriteAllTextAsync_ReplaceFails_LeavesNoTemporary()
    {
        Directory.CreateDirectory(_folder.FullPath(Name));

        // Awaited, not blocked on: its continuation needs the main thread of the engine
        Exception? thrown = null;
        try
        {
            await _storage.WriteAllTextAsync(_folder.FileName(Name), "new");
        }
        catch (Exception e)
        {
            thrown = e;
        }

        AssertThat(thrown).IsNotNull();
        AssertThat(_folder.Entries()).ContainsExactly(Name);
    }

    private static string TempName() => $"{Name}.{Environment.ProcessId}.tmp";
}
