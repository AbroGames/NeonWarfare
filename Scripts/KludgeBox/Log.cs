using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Godot;
using KludgeBox.Loggers;

namespace KludgeBox;

internal enum PrefixType
{
    Debug,
    Info,
    Warning,
    Error,
    Critical
}

public static class Log
{
    
    private static readonly HashSet<ILogger> _loggers = [];
    private static readonly string[] _prefixes;
    private static int PID;
    
    static Log()
    {
        PID = OS.GetProcessId();
        var rawPrefixes = Enum.GetNames<PrefixType>();
        var prefixes = new List<string>();
        
        var longestPrefix = rawPrefixes.Max(p => p.Length);
        
        foreach (var rawPrefix in rawPrefixes)
        {
            var sb = new StringBuilder();
            sb.Append('|');
            sb.Append(' ');
            sb.Append(rawPrefix.ToUpper());
            for (int i = 0; i < longestPrefix - rawPrefix.Length + 1; i++)
            {
                sb.Append(' ');
            }
            sb.Append('|');
            prefixes.Add(sb.ToString());
        }

        _prefixes = prefixes.ToArray();
        
        AddLogger(new DefaultLogger());
        Log.AddLogger(new FileLogger("/custom-logs"));
    }

    public static void AddLogger(ILogger logger)
    {
        _loggers.Add(logger);
    }

    public static void Debug(object msg = null)
    {
        foreach (var logger in _loggers) logger.Debug(Format(msg, PrefixType.Debug));
    }

    public static void Info(object msg = null)
    {
        foreach (var logger in _loggers) logger.Info(Format(msg, PrefixType.Info));
    }

    public static void Warning(object msg = null)
    {
        foreach (var logger in _loggers) logger.Warning(Format(msg, PrefixType.Warning));
    }

    public static void Error(object msg = null)
    {
        foreach (var logger in _loggers) logger.Error(Format(msg, PrefixType.Error));
    }

    public static void Critical(object msg = null)
    {
        foreach (var logger in _loggers) logger.Critical(Format(msg, PrefixType.Critical));
    }

    private static string Format(object msg = null, PrefixType prefix = PrefixType.Info)
    {
        if (msg is null) return null;
        string text = msg.ToString();
        
        var now = DateTime.Now;
        return $"[{PID:D6}] {now:dd.MM.yyyy HH:mm:ss.fff} {_prefixes[(int)prefix]} {text}";
    }
}



public interface ILogger
{
    void Debug(object msg = null);
    void Info(object msg = null);
    void Warning(object msg = null);
    void Error(object msg = null);
    void Critical(object msg = null);
}