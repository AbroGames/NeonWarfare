using NeonWarfare.Scenes.Worlds.Ports;

namespace NeonWarfare.GameTests.Worlds.Fixtures;

/// <summary>
/// Counts the reports instead of stopping the process.
/// </summary>
public class RecordingServerOwner : IServerOwner
{
    public int AdminLeftCount { get; private set; }

    public void AdminLeft() => AdminLeftCount++;
}
