using GdUnit4;
using NeonWarfare.GameTests.KludgeBox.Persistence.Fixtures;
using NeonWarfare.Scripts;
using NeonWarfare.Scripts.GlobalServices;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Scripts.GlobalServices;

// The real disk. A generated uid is new on every call, so a uid read back by the next Init is one written back
[TestSuite]
public class PlayerUidServiceTests
{
    private const string Name = "player-uid.json";
    private const string ValidUid = "Storeduidx-Ssssssssss";

    private TestUserFolder _folder = null!;

    [BeforeTest]
    public void SetUp()
    {
        _folder = new TestUserFolder();
    }

    [AfterTest]
    public void TearDown()
    {
        _folder.Dispose();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Init_Missing_AValidUid_WrittenBack()
    {
        PlayerUidService service = Create();

        service.Init();

        AssertThat(service.Get().IsValid).IsTrue();
        AssertThat(ReadBack()).IsEqual(service.Get());
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Init_InvalidUid_AValidUid_WrittenBack_TheBadCopyKept()
    {
        _folder.Write(Name, """{"Value":"not-a-uid"}""");
        PlayerUidService service = Create();

        service.Init();

        AssertThat(service.Get().IsValid).IsTrue();
        AssertThat(ReadBack()).IsEqual(service.Get());
        AssertThat(_folder.Read(Name + ".corrupt")).IsEqual("""{"Value":"not-a-uid"}""");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Set_ReadBackByTheNextInit()
    {
        PlayerUidService service = Create();
        service.Init();

        AssertThat(service.Set(new PlayerUid(ValidUid))).IsTrue();

        AssertThat(service.Get()).IsEqual(new PlayerUid(ValidUid));
        AssertThat(ReadBack()).IsEqual(new PlayerUid(ValidUid));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Set_InvalidUid_Rejected_TheStoredOneStays()
    {
        PlayerUidService service = Create();
        service.Init();
        PlayerUid stored = service.Get();

        AssertThat(service.Set(new PlayerUid("not-a-uid"))).IsFalse();

        AssertThat(service.Get()).IsEqual(stored);
        AssertThat(ReadBack()).IsEqual(stored);
    }

    private PlayerUidService Create() => new(_folder.FileName(Name));

    private PlayerUid ReadBack()
    {
        PlayerUidService next = Create();
        next.Init();
        return next.Get();
    }
}
