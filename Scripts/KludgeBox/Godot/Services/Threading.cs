using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace NeonWarfare.Scripts.KludgeBox.Godot.Services;

[GlobalClass]
public partial class Threading : Node
{
	private static Threading Instance
	{
		get
		{
			if (_instance == null)
			{
				// Check if we were loaded via Autoload
				_instance = ((SceneTree)Engine.GetMainLoop()).Root.GetNodeOrNull<Threading>(typeof(Threading).Name);
				if (_instance == null)
				{
					// Instantiate to root at runtime
					_instance = new Threading();
					_instance.Name = typeof(Threading).Name;
					_instance.CallDeferred(nameof(InitGlobalInstance));
				}
			}
			return _instance;
		}
	}
	
	private void InitGlobalInstance()
	{
		((SceneTree)Engine.GetMainLoop()).Root.AddChild(this);
	}
	
	private static Threading _instance;
	public static SynchronizationContext SyncContext { get; private set; }

	private static readonly List<Action> deferredTasks = new();
	private static readonly List<Action> deferredPhysicsTasks = new();
	
	private static readonly object deferredLock = new object();
	private static readonly object deferredPhysicsLock = new object();
	public override void _Ready()
	{
		SyncContext = SynchronizationContext.Current;
	}
	
	
	/// <summary>
	/// Executes a task in the main thread of the game.
	/// </summary>
	/// <param name="task">The action to be executed.</param>
	public static void SyncTask(Action task)
	{
		SyncContext.Post(state =>
		{
			task?.Invoke();
		}, null);
	}

	/// <summary>
	/// Executes a task asynchronously. The <paramref name="finishCallback"/> will be executed in the main thread after the task is completed.
	/// </summary>
	/// <param name="task">The action to be executed asynchronously.</param>
	/// <param name="finishCallback">Optional callback to be executed in the main thread upon completion of the task.</param>
	public static void RunAsync(Action task, Action finishCallback = null)
	{
		// Prepare the callback to be executed in the main thread.
		var callback = () => SyncTask(finishCallback);

		// Run the task asynchronously.
		var asyncTask = Task.Run(() =>
		{
			task?.Invoke();
			callback?.Invoke();
		});
	}

	/// <summary>
	/// Defers the execution of a task to the next frame.
	/// </summary>
	/// <param name="task">The action to be executed in the next frame.</param>
	public static void DeferTask(Action task)
	{
		// Lock the deferred tasks collection and add the task.
		lock (deferredLock)
		{
			deferredTasks.Add(task);
		}
	}

	/// <summary>
	/// Defers the execution of a task to the next physics processing cycle.
	/// </summary>
	/// <param name="task">The action to be executed in the next physics processing cycle.</param>
	public static void DeferPhysicsTask(Action task)
	{
		// Lock the deferred physics tasks collection and add the task.
		lock (deferredPhysicsLock)
		{
			deferredPhysicsTasks.Add(task);
		}
	}


	public override void _Process(double delta)
	{
		lock (deferredLock)
		{
			RunAll(deferredTasks);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		lock (deferredPhysicsLock)
		{
			RunAll(deferredPhysicsTasks);
		}
	}

	private static void RunAll(List<Action> tasks)
	{
		foreach (Action task in tasks)
		{
			try
			{
				task?.Invoke();
			}
			catch (Exception e)
			{
				Log.Error(e);
			}
		}
		
		tasks.Clear();
	}
}
