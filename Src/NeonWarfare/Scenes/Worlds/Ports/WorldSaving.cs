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

    /// <exception cref="ArgumentException"><paramref name="fileName"/> is invalid.</exception>
    public WorldSaving(SaveFileName fileName, int autoSaveIntervalMinutes)
    {
        if (!fileName.IsValid)
        {
            throw new ArgumentException($"The save file name '{fileName}' is invalid.", nameof(fileName));
        }
        ArgumentOutOfRangeException.ThrowIfNegative(autoSaveIntervalMinutes);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(autoSaveIntervalMinutes, MaxAutoSaveIntervalMinutes);
        FileName = fileName;
        AutoSaveIntervalMinutes = autoSaveIntervalMinutes;
    }

    public SaveFileName FileName { get; }

    /// <summary>0 turns the autosave off.</summary>
    public int AutoSaveIntervalMinutes { get; }
}
