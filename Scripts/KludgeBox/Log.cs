using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using KludgeBox.Loggers;
using Environment = System.Environment;

namespace NeonWarfare.Scripts.KludgeBox;

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
        AddLogger(new FileLogger("/custom-logs"));
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

    public static void Warning(object msg = null, bool printStackTrace = false)
    {
        foreach (var logger in _loggers) logger.Warning(Format(msg, PrefixType.Warning, printStackTrace));
    }

    public static void Error(object msg = null, bool printStackTrace = true)
    {
        foreach (var logger in _loggers) logger.Error(Format(msg, PrefixType.Error, printStackTrace));
    }

    public static void Critical(object msg = null, bool printStackTrace = true)
    {
        foreach (var logger in _loggers) logger.Critical(Format(msg, PrefixType.Critical, printStackTrace));
    }

    private static string Format(object msg = null, PrefixType prefix = PrefixType.Info, bool printStackTrace = false)
    {
        if (msg is null && !printStackTrace) return null;
        string text = msg.ToString() + (printStackTrace ? "\n" + Environment.StackTrace : "");
        
        var now = DateTime.Now;
        return $"[{PID:D6}] {now:dd.MM.yyyy HH:mm:ss.fff} {_prefixes[(int)prefix]} {text}";
    }
}



public interface ILogger
{
    void Debug(object msg = null);
    void Info(object msg = null);
    void Warning(object msg = null, Exception exception = null);
    void Error(object msg = null, Exception exception = null);
    void Critical(object msg = null, Exception exception = null);
}
