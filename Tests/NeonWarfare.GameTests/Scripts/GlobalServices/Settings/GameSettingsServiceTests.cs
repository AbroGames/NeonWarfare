using System.Text.Json;
using System.Text.RegularExpressions;
using GdUnit4;
using Godot;
using KludgeBox.Godot.Services;
using KludgeBox.Persistence;
using NeonWarfare.GameTests.KludgeBox.Persistence.Fixtures;
using NeonWarfare.GameTests.Scripts.GlobalServices.Settings.Fixtures;
using NeonWarfare.Scenes.Worlds.Ports;
using NeonWarfare.Scripts.GlobalServices.Settings;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Scripts.GlobalServices.Settings;

// The real disk
[TestSuite]
public class GameSettingsServiceTests
{
    private const string Name = "game-settings.json";

    private TestUserFolder _folder = null!;

    [BeforeTest]
    public void SetUp()
    {
        TestLocales.Load();
        _folder = new TestUserFolder();
    }

    [AfterTest]
    public void TearDown()
    {
        _folder.Dispose();
    }

    // The color has fields only, so without its converter it would not round-trip
    [TestCase]
    [RequireGodotRuntime]
    public void SetSettings_ReadBackByTheNextInit()
    {
        GameSettingsService first = Create();
        first.Init();
        GameSettings changed = first.GetSettings() with
        {
            PlayerNick = "Changed", PlayerColor = new Color(0.2f, 0.4f, 0.6f)
        };

        first.SetSettings(changed);

        GameSettingsService next = Create();
        next.Init();
        AssertThat(next.GetSettings().PlayerNick).IsEqual("Changed");
        AssertThat(next.GetSettings().PlayerColor.ToHtml()).IsEqual(changed.PlayerColor.ToHtml());
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Init_BlankNick_TheDefaultOne_TheOtherSettingsKept()
    {
        Json(Stored() with { PlayerNick = " " });

        AssertRepaired(Stored() with { PlayerNick = GameSettings.GetDefault().PlayerNick });

        AssertThat(_folder.Entries()).ContainsExactly(Name);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Init_UnknownLocale_TheDefaultOne_TheOtherSettingsKept()
    {
        Json(Stored() with { Locale = "xx" });

        AssertRepaired(Stored() with { Locale = I18NService.DefaultLocale });

        AssertThat(_folder.Entries()).ContainsExactly(Name);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Init_KnownLocaleInAnotherSpelling_ItsCode_TheOtherSettingsKept()
    {
        Json(Stored() with { Locale = "EN_us" });

        AssertRepaired(Stored() with { Locale = I18NService.DefaultLocale });
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Init_NoLocale_TheDefaultOne_TheOtherSettingsKept_TheBadCopyKept() =>
        AssertFieldReplaced(Json(Stored() with { Locale = null! }),
            Stored() with { Locale = I18NService.DefaultLocale });

    [TestCase]
    [RequireGodotRuntime]
    public void Init_BadColor_TheDefaultOne_TheOtherSettingsKept_TheBadCopyKept() =>
        AssertFieldReplaced(Regex.Replace(Json(Stored()), "\"PlayerColor\":\"[^\"]*\"", "\"PlayerColor\":\"zz\""),
            Stored() with { PlayerColor = GameSettings.GetDefault().PlayerColor });

    [TestCase]
    [RequireGodotRuntime]
    public void Init_NegativeAutoSaveInterval_TheDefaultOne_TheOtherSettingsKept()
    {
        Json(Stored() with { AutoSaveIntervalMinutes = -1 });

        AssertRepaired(Stored() with { AutoSaveIntervalMinutes = GameSettings.GetDefault().AutoSaveIntervalMinutes });
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Init_AutoSaveIntervalOverTheLongest_TheDefaultOne_TheOtherSettingsKept()
    {
        Json(Stored() with { AutoSaveIntervalMinutes = WorldSaving.MaxAutoSaveIntervalMinutes + 1 });

        AssertRepaired(Stored() with { AutoSaveIntervalMinutes = GameSettings.GetDefault().AutoSaveIntervalMinutes });
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Init_TheLongestAutoSaveInterval_Kept()
    {
        Json(Stored() with { AutoSaveIntervalMinutes = WorldSaving.MaxAutoSaveIntervalMinutes });

        AssertRepaired(Stored() with { AutoSaveIntervalMinutes = WorldSaving.MaxAutoSaveIntervalMinutes });
    }

    // What a new setting does to every file written before it
    [TestCase]
    [RequireGodotRuntime]
    public void Init_MissingSetting_TheDefaultOne_TheOtherSettingsKept()
    {
        _folder.Write(Name, Regex.Replace(Json(Stored()), "\"MasterVolume\":[0-9]+,", ""));

        AssertRepaired(Stored() with { MasterVolume = GameSettings.GetDefault().MasterVolume });

        AssertThat(_folder.Entries()).ContainsExactly(Name);
    }

    private GameSettingsService Create() => new(_folder.FileName(Name));

    // Differs from the defaults in every field the defaults do not generate. The interval is the lowest one kept.
    private static GameSettings Stored() => GameSettings.GetDefault() with
    {
        PlayerNick = "Stored", PlayerColor = Colors.Red, AutoSaveIntervalMinutes = 0, MasterVolume = 10
    };

    private string Json(GameSettings settings)
    {
        GameSettingsService writer = Create();
        writer.Init();
        writer.SetSettings(settings);
        return _folder.Read(Name);
    }

    // The file is read as it is, because the next Init would repair the settings again
    private void AssertRepaired(GameSettings expected)
    {
        GameSettingsService service = Create();

        service.Init();

        AssertThat(service.GetSettings()).IsEqual(expected);
        var file = new JsonFile<GameSettings>(_folder.FileName(Name),
            new JsonSerializerOptions { Converters = { new ColorJsonConverter() } });
        AssertThat(file.Read().Value).IsEqual(expected);
    }

    private void AssertFieldReplaced(string json, GameSettings expected)
    {
        _folder.Write(Name, json);

        AssertRepaired(expected);

        AssertThat(_folder.Read(Name + ".corrupt")).IsEqual(json);
    }
}
