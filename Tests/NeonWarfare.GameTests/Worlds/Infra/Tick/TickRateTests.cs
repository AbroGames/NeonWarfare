using GdUnit4;
using NeonWarfare.Scenes.Worlds.Infra.Tick;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds.Infra.Tick;

[TestSuite]
public class TickRateTests
{
    [TestCase]
    public void ToTicks_RoundsToTheNearestTick()
    {
        AssertThat(TickRate.ToTicks(1.5)).IsEqual(90);
        AssertThat(TickRate.ToTicks(0.1)).IsEqual(6);
        AssertThat(TickRate.ToTicks(0.009)).IsEqual(1);
        AssertThat(TickRate.ToTicks(0.008)).IsEqual(0);
        AssertThat(TickRate.ToTicks(0)).IsEqual(0);
    }

    [TestCase]
    public void ToTicks_NegativeNaNOrTooLong_Throws()
    {
        foreach (double seconds in new[] { -0.1, double.NaN, double.PositiveInfinity, int.MaxValue / 30.0 })
        {
            AssertThrown(() => TickRate.ToTicks(seconds)).IsInstanceOf<ArgumentOutOfRangeException>();
        }
    }
}
