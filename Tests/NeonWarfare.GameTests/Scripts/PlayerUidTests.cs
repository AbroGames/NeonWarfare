using GdUnit4;
using NeonWarfare.Scripts;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Scripts;

[TestSuite]
public class PlayerUidTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void Generate_IsValidAndUnique()
    {
        var uids = new HashSet<PlayerUid>();
        for (int i = 0; i < 100; i++)
        {
            PlayerUid uid = PlayerUid.Generate();
            AssertThat(uid.IsValid).OverrideFailureMessage($"{uid} is not valid").IsTrue();
            uids.Add(uid);
        }

        AssertThat(uids).HasSize(100);
    }
}
