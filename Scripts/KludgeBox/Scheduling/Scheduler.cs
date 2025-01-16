using System;
using System.Collections.Generic;

namespace NeonWarfare.Scripts.KludgeBox.Scheduling;

/// <summary>
/// Scheduler class.
/// </summary>
/// <remarks>
/// This is literally Bronuh's script for SpaceEngineers:
/// https://github.com/KeyJ148/SpaceEngineersScripts/blob/master/Scheduler/Scheduler.cs
/// </remarks>
public class Scheduler
{
	private List<SchedulerTask> _taskList = new List<SchedulerTask>();
	private long _tick = 0;
	private long _passedTime = 0;
	private int _lastId = 0;
	private bool _running = true;
	private Clock _clock;

	/// <summary>
	/// Creates a scheduler that executes assigned actions on a clock-based schedule.
	/// It also creates its own instance of Clock for counting.
	/// </summary>
	/// <param name="ticksPerSecond">Number of ticks to be produced per second</param>
	/// <remarks>
	/// The internal instance of Clock will not trigger global events in the KludgeEventBus.
	/// </remarks>
	public Scheduler(int ticksPerSecond)
	{
		_clock = new Clock(Tick);
		_clock.FireEvents = false;
		_clock.StartClock(ticksPerSecond);
	}

	/// <summary>
	/// Creates a scheduler that performs assigned actions on a tick-based schedule.
	/// </summary>
	/// <remarks>
	/// To make the scheduler created this way work, either manually call the Tick method,
	/// or attach it to an existing Clock instance.
	/// </remarks>
	public Scheduler() { }


	/// <summary>
	/// Creates a copy of the scheduler's task list, which contains references to tasks. Can be used to manage
	/// tasks (such as searching, stopping, resuming, cancelling, etc.).
	/// </summary>
	/// <returns>A copy of the task list</returns>
	public IEnumerable<SchedulerTask> GetTaskList()
	{
		foreach (var task in _taskList)
		{
			yield return task;
		}
	}

	public long GetCurrentTick()
	{
		return _tick;
	}

	public long GetPassedTime()
	{
		return _passedTime;
	}

	/// <summary>
	/// Creates a task that executes every few ticks.
	/// </summary>
	/// <param name="action">The action to be executed</param>
	/// <param name="interval">The interval in ticks</param>
	/// <param name="delay">(optional) The delay before the first execution. Default is 0</param>
	/// <param name="executionTimes">(optional) The number of task repetitions. Default is 1</param>
	/// <param name="doInfinitely">(optional) If true, the task will repeat until manually cancelled. Default is true</param>
	/// <returns>A reference to the created task for manual management</returns>
	public SchedulerTask ExecuteEveryNTicks(Action<double> action, int interval, int delay = 0,
		int executionTimes = 1, bool doInfinitely = true)
	{
		return AddTask(action, _tick + delay, interval, executionTimes, doInfinitely);
	}


	/// <summary>
	/// Executes a task once after a delay.
	/// </summary>
	/// <param name="action">The action to be executed</param>
	/// <param name="targetTick">The target tick for execution</param>
	/// <returns>A reference to the created task for manual management</returns>
	public SchedulerTask ExecuteAt(Action<double> action, long targetTick)
	{
		if(targetTick > _tick)
			return AddTask(action, targetTick);

		return null;
	}


	/// <summary>
	/// Executes a task once after a delay.
	/// </summary>
	/// <param name="action">The action to be executed</param>
	/// <param name="targetTick">The target tick for execution</param>
	/// <returns>A reference to the created task for manual management</returns>
	public SchedulerTask ExecuteAfter(Action<double> action, long delay)
	{
		return AddTask(action, _tick + delay);
	}


	/// <summary>
	/// General method for creating a task. Returns a reference to the task that can be enabled, disabled, cancelled,
	/// or assigned tags.
	/// </summary>
	/// <param name="action">The action to be executed</param>
	/// <param name="nextExecution">The tick number at which the task will be executed</param>
	/// <param name="interval">The number of ticks between executions</param>
	/// <param name="executionsRemaining">The number of remaining executions</param>
	/// <param name="doInfinitely">Whether to execute the task infinitely (ignores executionsRemaining)</param>
	/// <returns></returns>
	public SchedulerTask AddTask(Action<double> action, long nextExecution, int interval = 0, int executionsRemaining = 1, bool doInfinitely = false)
	{
		var task = new SchedulerTask(_lastId++, action, nextExecution, interval, executionsRemaining, doInfinitely);
		_taskList.Add(task);
		return task;
	}



	/// <summary>
	/// Starts the scheduler from the last tick.
	/// </summary>
	public void Start()
	{
		_running = true;
	}


	/// <summary>
	/// Pauses the scheduler.
	/// </summary>
	public void Stop()
	{
		_running = false;
	}


	/// <summary>
	/// Emulates a tick of the scheduler.
	/// </summary>
	/// <param name="dt">The delta time for the tick</param>
	public void Tick(double dt = 0)
	{
		_passedTime += (long)(dt * 1000);
		if (!_running)
			return;

		foreach (var task in new List<SchedulerTask>(_taskList))
		{
			TryExecute(task, dt);
		}
		_tick++;
	}



	/// <summary>
	/// Removes a task from the list, executes it, and adds the same task to the end of the list if it should be executed again.
	/// </summary>
	/// <param name="task">The task to try executing</param>
	/// <param name="dt">The delta time for the execution</param>
	private void TryExecute(SchedulerTask task, double dt)
	{
		if (task.MarkedForCancellation)
			return;

		if (task.NextExecutionAt <= _tick)
		{
			try
			{
				_taskList.Remove(task);
			}
			catch (Exception e)
			{
				//Log.Warn(e.Message);
			}
			task.Execute(_passedTime);
			if (task.DoInfinitely || task.ExecutionsRemaining > 0)
			{
				task.NextExecutionAt = _tick + task.Interval;
				_taskList.Add(task);
			}
		}
	}







	/// <summary>
	/// The task class for the scheduler.
	/// </summary>
	/// <remarks>
	/// This is literally Bronuh's script for SpaceEngineers:
	/// https://github.com/KeyJ148/SpaceEngineersScripts/blob/master/Scheduler/Scheduler.cs#L195
	/// </remarks>
	public class SchedulerTask
	{
		// List of tags for searching and filtering
		public List<string> Tags { get; set; } = new List<string>();

		// Unique task Id															 
		public int Id { get; private set; }

		// Remaining number of executions. Ignored if DoInfinitely = true
		public int ExecutionsRemaining { get; set; }
		public int Interval { get; set; }
		public long NextExecutionAt { get; set; }

		public bool DoInfinitely { get; set; }

		// Whether to execute the task when called. Controlled by Activate() and Deactivate() methods
		public bool Active { get; private set; } = true;

		// If true, the task will be removed during the next tick
		public bool MarkedForCancellation { get; private set; } = false;

		public string Name { get; private set; }

		private long _lastExecutionTime = 0;
		private long _previousExecutionTime = 0;

		public double Delta => (_previousExecutionTime - _lastExecutionTime) / 1000.0;

		private Action<double> _action;



		public SchedulerTask(int id, Action<double> action, long nextExecution, int interval = 0, int executionsRemaining = 1, bool doInfinitely = false)
		{
			Id = id;
			ExecutionsRemaining = executionsRemaining;
			_action = action;
			NextExecutionAt = nextExecution;
			Interval = interval;
			DoInfinitely = doInfinitely;
		}

		public SchedulerTask SetName(string name)
		{
			Name = name;
			return this;
		}

		/// <summary>
		/// Adds a tag to the task for future searching and filtering.
		/// </summary>
		/// <param name="tag">The tag to add</param>
		public SchedulerTask AddTag(string tag)
		{
			Tags.Add(tag);
			return this;
		}

		/// <summary>
		/// Adds tags to the task for future searching and filtering.
		/// </summary>
		/// <param name="tags">The tags to add</param>
		public void AddTags(params string[] tags)
		{
			Tags.AddRange(tags);
		}


		/// <summary>
		/// Executes the task and decreases the remaining repetitions counter if the task is active.
		/// </summary>
		/// <param name="currentTime">This must be a total work time of the scheduler</param>
		public void Execute(long currentTime)
		{
			_previousExecutionTime = _lastExecutionTime;
			_lastExecutionTime = currentTime;

			if (!Active)
				return;

			try
			{
				_action(Delta);
			}
			catch (Exception e)
			{
				//Log.Warn(e.Message);
			}
			if (!DoInfinitely)
				ExecutionsRemaining--;
		}



		/// <summary>
		/// The task will be removed from the scheduler during the next tick.
		/// </summary>
		public void Cancel()
		{
			MarkedForCancellation = true;
		}

		/// <summary>
		/// Enables task execution by the scheduler.
		/// </summary>
		public void Activate()
		{
			Active = true;
		}


		/// <summary>
		/// The task will not be executed until it is enabled by the Activate() method.
		/// </summary>
		public void Deactivate()
		{
			Active = false;
		}
	}
}
