using GdUnit4;
using NeonWarfare.Scenes.Worlds.Infra.Tick;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds.Infra.Tick;

[TestSuite]
public class TickTimerTests
{
    private static readonly TickTimer Timer = TickTimer.Start(100, 10);

    [TestCase]
    public void Reads_BeforeTheStart_AreOfAFreshTimer()
    {
        AssertThat(Timer.Elapsed(90)).IsEqual(0L);
        AssertThat(Timer.Remaining(90)).IsEqual(10L);
        AssertThat(Timer.Progress(90)).IsEqual(0d);
        AssertThat(Timer.IsFinished(90)).IsFalse();
    }

    [TestCase]
    public void Reads_WhileRunning_CountFromTheStart()
    {
        AssertThat(Timer.EndTick).IsEqual(110L);
        AssertThat(Timer.Elapsed(104)).IsEqual(4L);
        AssertThat(Timer.Remaining(104)).IsEqual(6L);
        AssertThat(Timer.Progress(104)).IsEqual(0.4);
        AssertThat(Timer.IsFinished(109)).IsFalse();
    }

    [TestCase]
    public void Reads_AtAndAfterTheEnd_AreFinished()
    {
        foreach (long tick in new long[] { 110, 500 })
        {
            AssertThat(Timer.Elapsed(tick)).IsEqual(10L);
            AssertThat(Timer.Remaining(tick)).IsEqual(0L);
            AssertThat(Timer.Progress(tick)).IsEqual(1d);
            AssertThat(Timer.IsFinished(tick)).IsTrue();
        }
    }

    // A ready cooldown and an inactive effect need no start
    [TestCase]
    public void Default_IsFinished()
    {
        TickTimer timer = default;

        AssertThat(timer.IsFinished(0)).IsTrue();
        AssertThat(timer.Remaining(0)).IsEqual(0L);
        AssertThat(timer.Progress(0)).IsEqual(1d);
    }

    [TestCase]
    public void ZeroDuration_IsFinishedAtItsStart()
    {
        TickTimer timer = TickTimer.Start(50, 0);

        AssertThat(timer.IsFinished(50)).IsTrue();
        AssertThat(timer.Progress(50)).IsEqual(1d);
    }

    [TestCase]
    public void Start_NegativeOrOverflowing_Throws()
    {
        AssertThrown(() => TickTimer.Start(-1, 10)).IsInstanceOf<ArgumentOutOfRangeException>();
        AssertThrown(() => TickTimer.Start(0, -1)).IsInstanceOf<ArgumentOutOfRangeException>();
        AssertThrown(() => TickTimer.Start(long.MaxValue, 1)).IsInstanceOf<ArgumentOutOfRangeException>();
    }
}
