using GdUnit4;
using NeonWarfare.GameTests.KludgeBox.Persistence.Fixtures;
using NeonWarfare.Scenes.Worlds.Features.Options;
using NeonWarfare.Scripts.GlobalServices;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Scripts.GlobalServices;

// The real disk
[TestSuite]
public class WorldOptionsFileServiceTests
{
    private const string Name = "child-world-options.json";

    private TestUserFolder _folder = null!;
    private WorldOptionsFileService _service = null!;

    [BeforeTest]
    public void SetUp()
    {
        _folder = new TestUserFolder();
        _service = new WorldOptionsFileService();
    }

    [AfterTest]
    public void TearDown()
    {
        _folder.Dispose();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Read_NoFileName_TheDefaults_NoFileWritten()
    {
        AssertThat(_service.Read(null!)).IsEqual(WorldOptions.GetDefault());

        AssertThat(_folder.Entries()).IsEmpty();
    }

    // What a client does for its child server. A multiplier of 0 turns friendly fire off, so it is valid
    [TestCase]
    [RequireGodotRuntime]
    public void Write_ReadBack_ZeroFriendlyFireKept()
    {
        var written = new WorldOptions(
            new FixedWorldOptions(allBuildingsUnlocked: true), new AdjustableWorldOptions(friendlyFireMultiplier: 0));

        AssertThat(_service.Write(_folder.FileName(Name), written)).IsTrue();

        AssertThat(_service.Read(_folder.FileName(Name))).IsEqual(written);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Read_Missing_TheDefaults_WrittenBack()
    {
        AssertThat(_service.Read(_folder.FileName(Name))).IsEqual(WorldOptions.GetDefault());

        AssertThat(_folder.Entries()).ContainsExactly(Name);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Read_NegativeFriendlyFire_ItsDefault_WrittenBack_TheOtherOptionsKept()
    {
        _service.Write(_folder.FileName(Name), new WorldOptions(
            new FixedWorldOptions(allBuildingsUnlocked: true), new AdjustableWorldOptions(friendlyFireMultiplier: -1)));

        WorldOptions read = _service.Read(_folder.FileName(Name));

        var expected = new WorldOptions(new FixedWorldOptions(allBuildingsUnlocked: true),
            AdjustableWorldOptions.GetDefault());
        AssertThat(read).IsEqual(expected);
        AssertThat(_folder.Read(Name)).Contains("\"FriendlyFireMultiplier\":1");
    }
}
