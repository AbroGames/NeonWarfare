using GdUnit4;
using NeonWarfare.Scenes.Worlds.Infra.Client.Tick;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds.Infra.Client.Tick;

[TestSuite]
public class ClientTickClockTests
{
    [TestCase]
    public void Sync_TickAhead_MovesTheClockToItAtOnce()
    {
        var clock = new ClientTickClock();
        clock.Sync(10);
        Advance(clock, 2);

        clock.Sync(20);

        AssertThat(clock.CurrentTick).IsEqual(20);
        AssertThat(Advance(clock, 2)).ContainsExactly(21, 22);
    }

    // A late packet: the clock waits for the server instead of going back
    [TestCase]
    public void Sync_TickBehind_SkipsTheLeadThenCountsAgain()
    {
        var clock = new ClientTickClock();
        clock.Sync(10);
        Advance(clock, 3);

        clock.Sync(11);

        AssertThat(clock.CurrentTick).IsEqual(13);
        AssertThat(Advance(clock, 4)).ContainsExactly(13, 13, 14, 15);
    }

    // The lead is taken from the latest packet, so two late packets in a row do not freeze the clock twice as long
    [TestCase]
    public void Sync_TwoTicksBehind_TheLeadIsOfTheLatest()
    {
        var clock = new ClientTickClock();
        clock.Sync(10);
        Advance(clock, 3);

        clock.Sync(11);
        clock.Sync(12);

        AssertThat(Advance(clock, 2)).ContainsExactly(13, 14);
    }

    [TestCase]
    public void Sync_TickAheadOfALead_CancelsIt()
    {
        var clock = new ClientTickClock();
        clock.Sync(10);
        Advance(clock, 3);
        clock.Sync(11);

        clock.Sync(15);

        AssertThat(Advance(clock, 2)).ContainsExactly(16, 17);
    }

    // The steady state: the packet of every tick catches up with the clock, so the client stops waiting
    [TestCase]
    public void Sync_TickEqualToTheClock_CancelsALead()
    {
        var clock = new ClientTickClock();
        clock.Sync(10);
        Advance(clock, 3);
        clock.Sync(11);

        clock.Sync(13);

        AssertThat(clock.CurrentTick).IsEqual(13);
        AssertThat(Advance(clock, 2)).ContainsExactly(14, 15);
    }

    [TestCase]
    public void Sync_NegativeTick_Throws()
    {
        AssertThrown(() => new ClientTickClock().Sync(-1)).IsInstanceOf<ArgumentOutOfRangeException>();
    }

    private static List<long> Advance(ClientTickClock clock, int steps)
    {
        List<long> ticks = [];
        for (int i = 0; i < steps; i++)
        {
            clock.Advance();
            ticks.Add(clock.CurrentTick);
        }
        return ticks;
    }
}
