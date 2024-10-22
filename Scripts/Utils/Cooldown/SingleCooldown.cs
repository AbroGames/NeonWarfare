using System;
using Godot;
using CooldownMode = KludgeBox.Scheduling.CooldownMode;

namespace NeonWarfare.Utils.Cooldown;

public class SingleCooldown
{
	
	//Duration of the cooldown in seconds.
	public double Duration { get; } = 0;
	
	//Gets elapsed time in seconds
	public double ElapsedTime => Duration - _timeLeft;

	//Gets the fraction of the cooldown completed, ranging from 0 to 1.
	public double FractionElapsedTime => ElapsedTime / Duration;
	
	//Cooldown ended, time left and actions executed
	public bool IsCompleted => _isCompleted;
	
	public event Action ActionWhenReady;
	
	private double _timeLeft = 0;
	private bool _isActivated = false;
	private bool _isCompleted = false;
	

	/// <summary>
	/// Initializes a new instance of the <see cref="Cooldown"/> class with the specified duration.
	/// </summary>
	/// <param name="duration">The duration of the cooldown in seconds.</param>
	public SingleCooldown(double duration, bool isActivated = false, bool isCompleted = false, Action actionWhenReady = null) 
	{
		if (duration == 0)
		{
			throw new ArgumentException("Duration can't be equal zero.");
		}
		
		Duration = duration;
		_timeLeft = duration;
		_isActivated = isActivated;
		_isCompleted = isCompleted;
		ActionWhenReady = actionWhenReady;
		
		if (isCompleted)
		{
			_timeLeft = 0;
		}
	}

	/// <summary>
	/// Updates the cooldown by a specified delta time 
	/// </summary>
	/// <param name="deltaTime">The time elapsed since the last update in seconds.</param>
	public void Update(double deltaTime)
	{
		if (!_isActivated) return;
		
		_timeLeft -= deltaTime;
	    if (_timeLeft <= 0)
	    {
		    _timeLeft = 0;
		    _isCompleted = true;
		    _isActivated = false;
		    ActionWhenReady?.Invoke();
	    }
	}
	
	/// <summary>
	/// Resetting the elapsed time to 0 and stop cooldowner.
	/// </summary>
	public void Reset()
	{
		_timeLeft = Duration;
		_isCompleted = false;
		_isActivated = false;
	}

	/// <summary>
	/// Resetting the elapsed time to 0 and activate cooldowner.
	/// </summary>
	public void Restart()
	{
		Reset();
		_isActivated = true;
	}
}