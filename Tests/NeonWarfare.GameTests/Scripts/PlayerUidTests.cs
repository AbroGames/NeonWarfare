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

    [TestCase]
    [RequireGodotRuntime]
    public void IsValid_TwoLetterGroupsJoinedByADash()
    {
        AssertThat(new PlayerUid("Aaaaaaaaaa-zzzzzzzzzZ").IsValid).IsTrue();
        AssertThat(default(PlayerUid).IsValid).IsFalse();
        AssertThat(new PlayerUid("Aaaaaaaaaa-Aaaaaaaaa").IsValid).IsFalse();
        AssertThat(new PlayerUid("Aaaaaaaaaa_Aaaaaaaaaa").IsValid).IsFalse();
        AssertThat(new PlayerUid("Aaaaaaaaa1-Aaaaaaaaaa").IsValid).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ToString_IsTheValue()
    {
        AssertThat(new PlayerUid("Aaaaaaaaaa-Bbbbbbbbbb").ToString()).IsEqual("Aaaaaaaaaa-Bbbbbbbbbb");
    }
}
