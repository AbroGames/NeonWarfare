using System;
using Serilog.Events;

namespace NeonWarfare.Scripts.GlobalServices.ServerLog;

/// <summary>
/// <c>Exception</c> is null when the record has none.
/// </summary>
public record ServerLogEntry(
    DateTimeOffset Timestamp,
    LogEventLevel Level,
    string Source,
    string Message,
    string Exception);
