using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using NeonWarfare.Scripts.GlobalServices.ServerLog;
using Serilog.Events;

namespace NeonWarfare.Scenes.Screen.ServerHud;

/// <summary>
/// Appends only the new records and removes the dropped ones, so the selection and the scroll survive a new record.
/// </summary>
public class ServerLogView(RichTextLabel label, ServerLogService log)
{
    private const string TimestampFormat = "HH:mm:ss.fff";

    private static readonly Color DebugColor = Colors.Gray;
    private static readonly Color WarningColor = Colors.Yellow;
    private static readonly Color ErrorColor = Colors.Red;

    // The paragraph count of every shown record, the oldest first, because an exception spans several paragraphs.
    private readonly Queue<int> _shownParagraphs = new();
    private long _shownVersion;

    public void Refresh()
    {
        (long version, IReadOnlyList<ServerLogEntry> entries) = log.ReadSince(_shownVersion);
        if (version - _shownVersion >= ServerLogService.Capacity)
        {
            label.Clear();
            _shownParagraphs.Clear();
        }
        _shownVersion = version;

        foreach (ServerLogEntry entry in entries)
        {
            Append(entry);
        }
        while (_shownParagraphs.Count > ServerLogService.Capacity)
        {
            RemoveOldest();
        }
    }

    // Every paragraph gets its own color, because RemoveParagraph drops a color with all the text inside it.
    // AddText takes the text as is, so a "[" in a message needs no BBCode escaping.
    private void Append(ServerLogEntry entry)
    {
        string[] lines = Format(entry).ReplaceLineEndings("\n").Split('\n');
        Color? color = GetColor(entry.Level);
        for (int i = 0; i < lines.Length; i++)
        {
            if (_shownParagraphs.Count > 0 || i > 0) label.Newline();
            if (color is { } pushed) label.PushColor(pushed);
            label.AddText(lines[i]);
            if (color != null) label.Pop();
        }
        _shownParagraphs.Enqueue(lines.Length);
    }

    private void RemoveOldest()
    {
        int paragraphs = _shownParagraphs.Dequeue();
        for (int i = 0; i < paragraphs; i++)
        {
            label.RemoveParagraph(0);
        }
    }

    private static string Format(ServerLogEntry entry)
    {
        string source = entry.Source == null ? "" : $" {entry.Source}:";
        string time = entry.Timestamp.ToString(TimestampFormat);
        string text = $"{time} {GetLevelName(entry.Level)}{source} {entry.Message}";
        return entry.Exception == null ? text : text + "\n" + entry.Exception;
    }

    private static Color? GetColor(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Verbose or LogEventLevel.Debug => DebugColor,
            LogEventLevel.Information => null,
            LogEventLevel.Warning => WarningColor,
            LogEventLevel.Error or LogEventLevel.Fatal => ErrorColor,
            _ => throw new UnreachableException()
        };
    }

    private static string GetLevelName(LogEventLevel level)
    {
        return level switch
        {
            LogEventLevel.Verbose => "VRB",
            LogEventLevel.Debug => "DBG",
            LogEventLevel.Information => "INF",
            LogEventLevel.Warning => "WRN",
            LogEventLevel.Error => "ERR",
            LogEventLevel.Fatal => "FTL",
            _ => throw new UnreachableException()
        };
    }
}
