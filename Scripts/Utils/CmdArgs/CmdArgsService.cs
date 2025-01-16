using System;
using System.Linq;
using Godot;
using KludgeBox;

namespace NeonWarfare.Scripts.Utils.CmdArgs;

public class CmdArgsService
{
    public static void LogCmdArgs()
    {
        if (!OS.GetCmdlineArgs().IsEmpty())
        {
            Log.Info("Cmd args: " + OS.GetCmdlineArgs().Join());
        }
        else
        {
            Log.Info("Cmd args is empty");
        }
    }

    public static bool ContainsInCmdArgs(string paramName)
    {
        return OS.GetCmdlineArgs().Contains(paramName);
    }
    
    public static string GetStringFromCmdArgs(string paramName, string defaultValue = null)
    {
        string arg = defaultValue;
        try
        {
            int argPos = OS.GetCmdlineArgs().ToList().IndexOf(paramName);
            if (argPos == -1)
            {
                Log.Info($"Arg {paramName} not setup.");
                return arg;
            }

            arg = OS.GetCmdlineArgs()[argPos + 1];
        }
        catch
        {
            Log.Warning($"Error while arg {paramName} setup.");
            return arg;
        }
        
        Log.Info($"{paramName}: {arg}");
        return arg;
    }

    public static int? GetIntFromCmdArgs(string paramName)
    {
        string argAsString = GetStringFromCmdArgs(paramName);
        int? arg = null;

        try
        {
            if (argAsString != null)
            {
                arg = Convert.ToInt32(argAsString);
            }
        }
        catch (Exception ex) when (ex is FormatException || ex is OverflowException)
        {
            Log.Warning($"Arg {paramName} can't convert to Int32");
            return arg;
        }

        return arg;
    }
    
    public static int GetIntFromCmdArgs(string paramName, int defaultValue)
    {
        string argAsString = GetStringFromCmdArgs(paramName, defaultValue.ToString());
        int arg = defaultValue;

        try
        {
            if (argAsString != null)
            {
                arg = Convert.ToInt32(argAsString);
            }
        }
        catch (Exception ex) when (ex is FormatException || ex is OverflowException)
        {
            Log.Warning($"Arg {paramName} can't convert to Int32");
            return arg;
        }

        return arg;
    }
}
