using System;
using System.Collections.Generic;
using static KludgeBox.Scheduling.ClockEvents;

namespace KludgeBox.Scheduling;

/// <summary>
/// This class provides basic clock functionality.
/// </summary>
public class Clock
{
	public static event Action<Clock, TickEvent> Tick = null;
		
	public static int DefaultTps { get; set; } = 60;

	/// <summary>
	/// If true then will fire tick events
	/// </summary>
	public bool FireEvents { get; set; } = true;

	public bool IsPaused { get; private set; } = false;

	/// <summary>
	/// Shows how many ticks passed since the clock started
	/// </summary>
	public long CurrentTick { get; private set; } = 0;

	/// <summary>
	/// Name of the clock. Produced TickEvents will contain the same name.
	/// </summary>
	public string ClockName { get; private set; }

	private Action<double> TickAction;
	private bool _first = true;
	private DateTime _lastTickTime;
	private System.Timers.Timer _timer;
	private List<Scheduler> _attachedSchedulers = new List<Scheduler>();

	/// <summary>
	/// Provided action will get the delta as double parameter.
	/// Delta represents time in seconds since last tick.
	/// </summary>
	/// <param name="action"></param>
	public Clock(Action<double> action, string name = null)
	{
		TickAction = action;
		ClockName = name;
	}

	public void Pause() => IsPaused = true;
	public void Unpause() => IsPaused = false;

	/// <summary>
	/// All of the attached shcedulers will be ticked with the same TPS.
	/// </summary>
	/// <param name="scheduler"></param>
	public void AttachScheduler(Scheduler scheduler)
	{
		_attachedSchedulers.Add(scheduler);
	}

	/// <summary>
	/// Starts clock with specified ticks per second.
	/// </summary>
	/// <param name="ticksPerSecond"></param>
	public void StartClock(int ticksPerSecond = 0)
	{
		// Invalid value will be replaced with default 60 TPS
		if (ticksPerSecond <= 0)
			ticksPerSecond = DefaultTps;

		// Start timer
		_timer = new System.Timers.Timer(1000.0 / ticksPerSecond);
		_timer.Elapsed += (sender, args) => DoTick();
		_timer.Start();
	}


	private void DoTick()
	{
		if (IsPaused)
			return;

		// Increase current tick
		CurrentTick++;

		// Get time since last tick
		double interval = GetInterval();

		// Fire tick start event
		if (FireEvents)
			Tick?.Invoke(this, new TickEvent(TickStage.Start, _first ? 0 : interval, CurrentTick, ClockName));

		// Execute main action
		TickAction?.Invoke(interval);
		// Emulate tick for every attached scheduler
		foreach (Scheduler scheduler in _attachedSchedulers)
			scheduler.Tick(interval);

		// Fire tick end event
		if (FireEvents)
			Tick?.Invoke(this, new TickEvent(TickStage.End, _first? 0 : interval, CurrentTick, ClockName));

		if (_first)
			_first = false;
	}

	// Returns time in seconds since the last tick
	private double GetInterval()
	{
		DateTime now = DateTime.Now;
		double interval = _lastTickTime == default ? 0 : (now - _lastTickTime).TotalSeconds;
		_lastTickTime = now;
		return interval;
	}
}