using System;
using System.Diagnostics;
using Godot;
using KludgeBox;

namespace NeonWarfare.Scripts.Utils;

public static class ExceptionHandlerService
{
    public static void AddExceptionHandlerForUnhandledException()
    {
        AppDomain.CurrentDomain.UnhandledException += HandleException;
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
