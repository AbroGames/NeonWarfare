using System;

namespace NeonWarfare.Scenes.World.Service.Command;

public static class CommandProcessorExtensions
{
    
    /// <summary>
    /// Cut all from start to first space (include) from string.<br/>
    /// If string doesn't contain space, them return empty string
    /// </summary>
    public static string CutCommand(this ICommandProcessor processor, string command)
    {
        return command.Substring(Math.Min(command.Length, processor.GetCommand().Length + 1));
    }

    public static string[] ParseParams(this ICommandProcessor processor, string command)
    {
        return processor.CutCommand(command).Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }
}