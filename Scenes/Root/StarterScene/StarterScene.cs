using System;
using System.Diagnostics;
using Godot;
using KludgeBox;
using NeonWarfare.Net;
using NeonWarfare.Utils;

namespace NeonWarfare;

public partial class StarterScene : Node
{
	
	[Export] [NotNull] public PackedScene ClientRoot { get; private set; }
	[Export] [NotNull] public PackedScene ServerRoot { get; private set; }
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
        CallDeferred(nameof(StartGame));
	}

	private void StartGame()
	{
		AppDomain.CurrentDomain.UnhandledException += HandleException;
		PackedScene rootScene = CmdArgsService.ContainsInCmdArgs(ServerParams.ServerFlag) ? ServerRoot : ClientRoot;
		GetTree().ChangeSceneToPacked(rootScene);
	}

	private static void HandleException(object sender, UnhandledExceptionEventArgs args)
	{
		// If logging will produce unhandled exception then we fucked up, so we need try/catch here.
		try
		{
			if (args.ExceptionObject is not Exception exception) return;
			
			Log.Error(exception.ToString());
		}
		catch (Exception e)
		{
			// Use GD.Print instead of Log.Error to avoid infinite recursion.
			GD.Print($"Unexpected exception was thrown while handling unhandled exception: {e}");
		}
		
		Debugger.Break();
	}
}