using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KludgeBox.Logging;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace NeonWarfare.Scripts.GlobalServices.ServerLog;

/// <summary>
/// The last log records of the process, for the window of the dedicated server.
/// It stays empty until <see cref="Init"/>.
/// </summary>
public class ServerLogService : ILogEventSink
{
    public const int Capacity = 1000;
    
    private const string SourceProperty = "ShortSourceContext";
    private const string MessageTemplate = "{Message:lj}";

    private readonly MessageTemplateTextFormatter _messageFormatter = new(MessageTemplate);
    private readonly Queue<ServerLogEntry> _entries = new();
    private readonly object _lock = new();
    private long _version;

    /// <summary>
    /// Returns the kept records that came after <paramref name="version"/>, the oldest first, and the version to pass
    /// next time. A version is the number of records ever added, so a reader far behind gets only the kept ones.
    /// </summary>
    public (long Version, IReadOnlyList<ServerLogEntry> Entries) ReadSince(long version)
    {
        lock (_lock)
        {
            long unread = Math.Clamp(_version - version, 0, _entries.Count);
            return (_version, _entries.Skip(_entries.Count - (int)unread).ToArray());
        }
    }

    public void Init()
    {
        LogFactory.Subscribe(this);
    }

    /// <summary>
    /// Called from the async workers of the loggers.
    /// </summary>
    public void Emit(LogEvent logEvent)
    {
        ServerLogEntry entry = new(
            logEvent.Timestamp,
            logEvent.Level,
            GetSource(logEvent),
            RenderMessage(logEvent),
            logEvent.Exception?.ToString());

        lock (_lock)
        {
            if (_entries.Count == Capacity) _entries.Dequeue();
            _entries.Enqueue(entry);
            _version++;
        }
    }

    private string RenderMessage(LogEvent logEvent)
    {
        using StringWriter writer = new();
        _messageFormatter.Format(logEvent, writer);
        return writer.ToString();
    }

    private static string GetSource(LogEvent logEvent)
    {
        return logEvent.Properties.TryGetValue(SourceProperty, out LogEventPropertyValue value)
               && value is ScalarValue { Value: string source }
            ? source
            : null;
    }
}
