using GdUnit4;
using KludgeBox.Godot.Services;
using NeonWarfare.GameTests.KludgeBox.Persistence.Fixtures;
using NeonWarfare.GameTests.Scripts.GlobalServices.Settings.Fixtures;
using NeonWarfare.Scripts.GlobalServices.Settings;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Scripts.GlobalServices.Settings;

// The real disk
[TestSuite]
public class DedicatedServerSettingsServiceTests
{
    private const string Name = "child-server-settings.json";

    private TestUserFolder _folder = null!;
    private DedicatedServerSettingsService _service = null!;

    [BeforeTest]
    public void SetUp()
    {
        TestLocales.Load();
        _folder = new TestUserFolder();
        _service = new DedicatedServerSettingsService();
    }

    [AfterTest]
    public void TearDown()
    {
        _folder.Dispose();
    }

    // What a client does for its child server
    [TestCase]
    [RequireGodotRuntime]
    public void Write_ReadByInitOfThatFile()
    {
        var written = new DedicatedServerSettings(I18NService.DefaultLocale, AutoSaveEnabled: false);

        AssertThat(_service.Write(_folder.FileName(Name), written)).IsTrue();

        _service.Init(_folder.FileName(Name));
        AssertThat(_service.GetSettings()).IsEqual(written);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Init_Missing_DefaultsWrittenBack()
    {
        _service.Init(_folder.FileName(Name));

        AssertThat(_service.GetSettings()).IsEqual(DedicatedServerSettings.GetDefault());
        AssertThat(_folder.Entries()).ContainsExactly(Name);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Init_UnknownLocale_TheDefaultOne_WrittenBack_TheAutoSaveKept()
    {
        _service.Write(_folder.FileName(Name), new DedicatedServerSettings("xx", AutoSaveEnabled: false));

        _service.Init(_folder.FileName(Name));

        var expected = new DedicatedServerSettings(I18NService.DefaultLocale, AutoSaveEnabled: false);
        AssertThat(_service.GetSettings()).IsEqual(expected);
        AssertThat(_folder.Read(Name)).Contains($"\"Locale\":\"{I18NService.DefaultLocale}\"");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Init_KnownLocaleInAnotherSpelling_ItsCode_WrittenBack()
    {
        _service.Write(_folder.FileName(Name), new DedicatedServerSettings("EN_us", AutoSaveEnabled: false));

        _service.Init(_folder.FileName(Name));

        var expected = new DedicatedServerSettings(I18NService.DefaultLocale, AutoSaveEnabled: false);
        AssertThat(_service.GetSettings()).IsEqual(expected);
        AssertThat(_folder.Read(Name)).Contains($"\"Locale\":\"{I18NService.DefaultLocale}\"");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Init_BadLocale_TheDefaultOne_TheAutoSaveKept()
    {
        _folder.Write(Name, """{"Locale":5,"AutoSaveEnabled":false}""");

        _service.Init(_folder.FileName(Name));

        var expected = new DedicatedServerSettings(I18NService.DefaultLocale, AutoSaveEnabled: false);
        AssertThat(_service.GetSettings()).IsEqual(expected);
    }
}
