using System;

namespace NeonWarfare.Scripts.KludgeBox.Scheduling;

/// <summary>
/// Represents a simple ticking timer that triggers an event when reaching the specified tick delay.
/// </summary>
public class TickingTimer
{
    /// <summary>
    /// Gets or sets the current tick count.
    /// </summary>
    public int CurrentTicks { get; set; } = 0;

    /// <summary>
    /// Gets or sets the delay between ticks.
    /// </summary>
    public int TickDelay { get; set; } = 1;

    /// <summary>
    /// Gets the remaining ticks until the next trigger.
    /// </summary>
    public int TicksLeft => TickDelay - CurrentTicks;

    /// <summary>
    /// Event triggered when the timer reaches the specified tick delay.
    /// </summary>
    public event Action OnTimerElapsed = null;

    /// <summary>
    /// Advances the timer by one tick and triggers the event if the specified delay is reached.
    /// </summary>
    public void AdvanceTick()
    {
        CurrentTicks++;
        if (CurrentTicks >= TickDelay)
        {
            CurrentTicks = 0;
            OnTimerElapsed?.Invoke();
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TickingTimer"/> class with the specified tick delay.
    /// </summary>
    /// <param name="tickDelay">The delay between ticks.</param>
    public TickingTimer(int tickDelay)
    {
        TickDelay = tickDelay;
    }
}
