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
    public void TryParse_Valid_IsTheUid()
    {
        AssertThat(PlayerUid.TryParse("Abcdefghij-KLMNOPQRST", out PlayerUid uid)).IsTrue();
        AssertThat(uid).IsEqual(new PlayerUid("Abcdefghij-KLMNOPQRST"));
    }

    [TestCase]
    public void TryParse_Invalid_IsDefault()
    {
        string?[] values =
            [null, "", "alice", "Abcdefghij_KLMNOPQRST", "Abcdefghi1-KLMNOPQRST", "Abcdefghij-KLMNOPQRSTU"];

        foreach (string? value in values)
        {
            AssertThat(PlayerUid.TryParse(value!, out PlayerUid uid))
                .OverrideFailureMessage($"'{value}' parsed").IsFalse();
            AssertThat(uid).IsEqual(default(PlayerUid));
        }
    }
}
