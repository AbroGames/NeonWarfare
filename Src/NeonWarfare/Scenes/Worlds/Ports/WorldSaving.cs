using System;
using NeonWarfare.Scenes.Worlds.Infra.Tick;

namespace NeonWarfare.Scenes.Worlds.Ports;

/// <summary>
/// The file a server World saves to from its start, and how often it autosaves there.
/// A "save as" moves both the autosave and the save on exit to another file.
/// </summary>
public sealed record WorldSaving
{
    /// <summary>The longest interval whose ticks fit into an <c>int</c>.</summary>
    public const int MaxAutoSaveIntervalMinutes = int.MaxValue / (60 * TickRate.TicksPerSecond);

    public WorldSaving(string fileName, int autoSaveIntervalMinutes)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        ArgumentOutOfRangeException.ThrowIfNegative(autoSaveIntervalMinutes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(autoSaveIntervalMinutes, MaxAutoSaveIntervalMinutes);
        FileName = fileName;
        AutoSaveIntervalMinutes = autoSaveIntervalMinutes;
    }

    public string FileName { get; }

    /// <summary>0 turns the autosave off.</summary>
    public int AutoSaveIntervalMinutes { get; }
}
