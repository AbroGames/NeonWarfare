using System;

namespace KludgeBox.Scheduling;

/// <summary>
/// Specifies the mode of a cooldown mechanism.
/// </summary>
public enum CooldownMode
{
	/// <summary>
	/// The cooldown restarts automatically after reaching the duration.
	/// </summary>
	Cyclic,

	/// <summary>
	/// The cooldown does not restart automatically after reaching the duration.
	/// </summary>
	Single
}
	
/// <summary>
/// Represents a cooldown mechanism that tracks the elapsed time and provides tick-based functionality.
/// </summary>
public class Cooldown
{
	private double _elapsedTime = 0;

	public CooldownMode Mode { get; set; } = CooldownMode.Single;

	/// <summary>
	/// Gets or sets the duration of the cooldown in seconds.
	/// </summary>
	public double Duration { get; set; } = 0;

	/// <summary>
	/// Gets the fraction of the cooldown completed, ranging from 0 to 1.
	/// </summary>
	public float FractionElapsed => (float)(_elapsedTime / Duration);

	public double TimeLeft => (1-FractionElapsed) * Duration;

	public double TimeElapsed
	{
		get => _elapsedTime;
		set => _elapsedTime = value;
	}

	public event Action Ready;

	/// <summary>
	/// Initializes a new instance of the <see cref="Cooldown"/> class with a default duration of 0 seconds.
	/// </summary>
	public Cooldown() { }

	/// <summary>
	/// Initializes a new instance of the <see cref="Cooldown"/> class with the specified duration.
	/// </summary>
	/// <param name="duration">The duration of the cooldown in seconds.</param>
	public Cooldown(double duration, CooldownMode mode = CooldownMode.Cyclic, bool isReady = false) 
	{
		Duration = duration; 
		Mode = mode;
		if (isReady)
		{
			_elapsedTime = duration;
		}
	}

	public Cooldown WithCallback(Action action)
	{
		Ready += action;
		return this;
	}

	/// <summary>
	/// Updates the cooldown by a specified delta time and returns the number of ticks that occurred.
	/// </summary>
	/// <param name="deltaTime">The time elapsed since the last update in seconds.</param>
	/// <returns>The number of ticks that occurred during the update.</returns>
	public int Update(double deltaTime)
	{
		int ticks = 0;
		if(Mode is CooldownMode.Cyclic)
		{
			_elapsedTime += deltaTime;
			ticks = (int)(_elapsedTime / Duration);
			_elapsedTime -= ticks * Duration;
			if(ticks>0)
				for (int i = 0; i < ticks; i++)
				{
					Ready?.Invoke();
				}
		}
		else
		{
			_elapsedTime = Maths.Clamp(_elapsedTime + deltaTime, 0, Duration);
			if (_elapsedTime >= Duration)
			{
				ticks = 1;
				Ready?.Invoke();
			}
					
		}
		return ticks;
	}

	/// <summary>
	/// Restarts the cooldown, resetting the elapsed time to 0.
	/// </summary>
	public void Restart()
	{
		_elapsedTime = 0;
	}
	

	public bool Use()
	{
		bool canUse = _elapsedTime >= Duration;

		if (canUse && (Mode is CooldownMode.Single))
			Restart();

		return canUse;
	}
}