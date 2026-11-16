using NeonWarfare.Scenes.World.Infra.ServerNetwork;

namespace NeonWarfare.GameTests.World.Fixtures;

/// <summary>
/// Counts the reports instead of stopping the process.
/// </summary>
public class RecordingDedicatedServerOwner : IDedicatedServerOwner
{
    public int AdminLeftCount { get; private set; }

    public void AdminLeft() => AdminLeftCount++;
}
