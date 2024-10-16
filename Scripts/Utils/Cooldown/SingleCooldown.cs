using System;
using Godot;
using CooldownMode = KludgeBox.Scheduling.CooldownMode;

namespace NeonWarfare.Utils.Cooldown;

public class SingleCooldown
{
	
	//Duration of the cooldown in seconds.
	public double Duration { get; set; } = 0;
	
	//Gets elapsed time in seconds
	public double ElapsedTime => Duration - _timeLeft;

	//Gets the fraction of the cooldown completed, ranging from 0 to 1.
	public double FractionElapsedTime => ElapsedTime / Duration;
	
	public event Action ActionsWhenReady;
	
	private double _timeLeft = 0;
	private bool _isActivated = false;
	private bool _isReady = true;
	

	/// <summary>
	/// Initializes a new instance of the <see cref="Cooldown"/> class with the specified duration.
	/// </summary>
	/// <param name="duration">The duration of the cooldown in seconds.</param>
	public SingleCooldown(double duration, bool isActivated = false, bool isReady = false, Action actionWhenReady = null) 
	{
		if (duration == 0)
		{
			throw new ArgumentException("Duration can't be equal zero.");
		}
		
		Duration = duration;
		_timeLeft = duration;
		_isActivated = isActivated;
		_isReady = isReady;
		if (isReady)
		{
			_timeLeft = 0;
		}

		if (actionWhenReady != null)
		{
			ActionsWhenReady += actionWhenReady;
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
	    if (_timeLeft < 0)
	    {
		    _timeLeft = 0;
	    }
	    
	    if (_timeLeft == 0)
	    {
		    _isReady = true;
		    ActionsWhenReady?.Invoke();
		    _isActivated = false;
	    }
	}
	
	/// <summary>
	/// Resetting the elapsed time to 0 and stop cooldowner.
	/// </summary>
	public void Reset()
	{
		_timeLeft = Duration;
		_isReady = false;
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
	
	public bool TryUse()
	{
		bool canUse = _isActivated && _isReady;
		if (canUse)
		{
			ActionsWhenReady?.Invoke();
			_isActivated = false;
		}
		
		return canUse;
	}
}