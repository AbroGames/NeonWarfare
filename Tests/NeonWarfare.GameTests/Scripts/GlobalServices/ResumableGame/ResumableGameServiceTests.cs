using GdUnit4;
using NeonWarfare.GameTests.KludgeBox.Persistence.Fixtures;
using NeonWarfare.Scripts;
using NeonWarfare.Scripts.GlobalServices.ResumableGame;
using static GdUnit4.Assertions;
using LastGame = NeonWarfare.Scripts.GlobalServices.ResumableGame.ResumableGame;

namespace NeonWarfare.GameTests.Scripts.GlobalServices.ResumableGame;

[TestSuite]
public class ResumableGameServiceTests
{
    private const string Name = "resume-game.json";

    private TestUserFolder _folder = null!;
    private ResumableGameService _service = null!;

    [BeforeTest]
    public void SetUp()
    {
        _folder = new TestUserFolder();
        _service = new ResumableGameService(_folder.FileName(Name));
    }

    [AfterTest]
    public void TearDown()
    {
        _folder.Dispose();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void GetLastGame_Missing_IsNone_WritesNothing()
    {
        AssertThat(_service.GetLastGame()).IsEqual(LastGame.GetNone());

        AssertThat(_folder.Entries()).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void GetLastGame_ServerWithoutPort_IsNone_WritesNothing() =>
        AssertNone("""{"Type":"CreateServer","SaveName":"save","Host":null,"Port":null,"IsDedicated":true}""");

    [TestCase]
    [RequireGodotRuntime]
    public void GetLastGame_SingleplayerWithoutSave_IsNone_WritesNothing() =>
        AssertNone("""{"Type":"RunSingleplayer","SaveName":null,"Host":null,"Port":null,"IsDedicated":null}""");

    // "Continue" with an invalid name would fail at the start, so it is hidden
    [TestCase]
    [RequireGodotRuntime]
    public void GetLastGame_InvalidSaveName_IsNone_WritesNothing() =>
        AssertNone("""{"Type":"RunSingleplayer","SaveName":"a/b","Host":null,"Port":null,"IsDedicated":null}""");

    [TestCase]
    [RequireGodotRuntime]
    public void GetLastGame_SingleplayerWithAValidSaveName_IsTheGame()
    {
        _folder.Write(Name,
            """{"Type":"RunSingleplayer","SaveName":"save","Host":null,"Port":null,"IsDedicated":null}""");

        AssertThat(_service.GetLastGame()).IsEqual(LastGame.GetSingleplayer(new SaveFileName("save")));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void GetLastGame_ConnectionWithoutHost_IsNone_WritesNothing() =>
        AssertNone("""{"Type":"ConnectToServer","SaveName":null,"Host":null,"Port":25566,"IsDedicated":null}""");

    [TestCase]
    [RequireGodotRuntime]
    public void GetLastGame_UnknownType_IsNone_WritesNothing() =>
        AssertNone("""{"Type":"Unknown","SaveName":"save","Host":"host","Port":25566,"IsDedicated":true}""");

    [TestCase]
    [RequireGodotRuntime]
    public void SetLastGame_ThenGetLastGame_RoundTrips()
    {
        LastGame game = LastGame.GetCreateServer(new SaveFileName("save"), 25566, isDedicated: true);

        _service.SetLastGame(game);

        AssertThat(new ResumableGameService(_folder.FileName(Name)).GetLastGame()).IsEqual(game);
    }

    private void AssertNone(string json)
    {
        _folder.Write(Name, json);

        AssertThat(_service.GetLastGame()).IsEqual(LastGame.GetNone());

        AssertThat(_folder.Entries()).ContainsExactly(Name + ".corrupt");
    }
}
