using GdUnit4;
using KludgeBox.Persistence;
using NeonWarfare.GameTests.Scripts.GlobalServices.Settings.Fixtures;
using NeonWarfare.Scripts;
using NeonWarfare.Scripts.GlobalServices;
using NeonWarfare.Scripts.GlobalServices.Settings;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Scripts.GlobalServices;

// The service reads Services.GameSettings and Services.PlayerUid, which keep their files in user:// of the test
// project, so the files are removed before and after every test
[TestSuite]
public class LocalPlayerServiceTests
{
    private static readonly string[] FileNames =
    [
        "game-settings.json", "game-settings.json.corrupt", "player-uid.json", "player-uid.json.corrupt"
    ];

    private const string OverrideUid = "Overridden-Oooooooooo";

    private GameSettings _stored = null!;
    private LocalPlayerService _localPlayer = null!;

    [BeforeTest]
    public void SetUp()
    {
        TestLocales.Load();
        RemoveFiles();
        Services.PlayerUid.Init();
        Services.GameSettings.Init();
        _stored = Services.GameSettings.GetSettings();
        _localPlayer = new LocalPlayerService();
    }

    [AfterTest]
    public void TearDown()
    {
        RemoveFiles();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Read_HasTheOverrides_OverTheStoredColor()
    {
        _localPlayer.SetOverrides("Temp", OverrideUid);

        AssertThat(_localPlayer.Read().Uid).IsEqual(new PlayerUid(OverrideUid));
        AssertThat(_localPlayer.Read().Nick).IsEqual("Temp");
        AssertThat(_localPlayer.Read().Color).IsEqual(_stored.PlayerColor);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Read_WithoutOverrides_IsTheStoredPlayer()
    {
        _localPlayer.SetOverrides(null, null);

        AssertThat(_localPlayer.Read().Uid).IsEqual(Services.PlayerUid.Get());
        AssertThat(_localPlayer.Read().Nick).IsEqual(_stored.PlayerNick);
    }

    // What a settings page does: a draft from the stored settings, saved back
    [TestCase]
    [RequireGodotRuntime]
    public void SettingsSavedWithOverrides_TheFileKeepsTheStoredNick()
    {
        _localPlayer.SetOverrides("Temp", OverrideUid);

        Services.GameSettings.SetSettings(Services.GameSettings.GetSettings() with { MasterVolume = 50 });

        var next = new GameSettingsService();
        next.Init();
        AssertThat(next.GetSettings()).IsEqual(_stored with { MasterVolume = 50 });
    }

    private static void RemoveFiles()
    {
        var storage = new UserStorage();
        foreach (string fileName in FileNames) storage.DeleteFile(fileName);
    }
}
