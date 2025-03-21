using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NeonWarfare.Scenes.Game.ServerGame.ServerCommandsService;

public record Command(
    string Name,
    string Description,
    bool IsAdminOnly,
    Action<Command, CommandArgs> Action,
    string[] Aliases
    );
    
public class CommandArgs
{
    /// <summary>
    /// Contains "command" from "/command arg1 arg2 "arg3 and still arg3" arg4"
    /// </summary>
    public string Command { get; }
    
    /// <summary>
    /// Contains ["arg1", "arg2", "arg3 and still arg3", "arg4"] from "/command arg1 arg2 "arg3 and still arg3" arg4"
    /// </summary>
    public string[] Arguments { get; }
    
    /// <summary>
    /// Contains "arg1 arg2 "arg3 and still arg3" arg4" from "/command arg1 arg2 "arg3 and still arg3" arg4"
    /// </summary>
    public string RawArguments { get; }
    
    /// <summary>
    /// Contains whole "/command arg1 arg2 "arg3 and still arg3" arg4"
    /// </summary>
    public string RawMessage { get; }

    public CommandArgs(string rawMessage)
    {
        RawMessage = rawMessage.Trim();
        
        if (string.IsNullOrEmpty(RawMessage))
        {
            throw new ArgumentException("Command message cannot be empty.");
        }
        
        string pattern = "\\\".*?\\\"|\\S+"; // Matches quoted strings or single words
        var matches = Regex.Matches(RawMessage, pattern);
        
        List<string> parts = new List<string>();
        foreach (Match match in matches)
        {
            string arg = match.Value;
            if (arg.StartsWith("\"") && arg.EndsWith("\""))
            {
                arg = arg.Substring(1, arg.Length - 2); // Remove surrounding quotes
            }
            parts.Add(arg);
        }
        
        if (parts.Count == 0)
        {
            throw new ArgumentException("Invalid command format.");
        }
        
        Command = parts[0].StartsWith("/") ? parts[0].Substring(1) : parts[0]; // Remove leading /
        if(String.IsNullOrWhiteSpace(Command))
            throw new ArgumentException("Command can not be empty");
        
        Arguments = parts.Count > 1 ? parts.GetRange(1, parts.Count - 1).ToArray() : Array.Empty<string>();
        RawArguments = Arguments.Length > 0 ? RawMessage.Substring(RawMessage.IndexOf(' ') + 1).Trim() : string.Empty;
    }
}