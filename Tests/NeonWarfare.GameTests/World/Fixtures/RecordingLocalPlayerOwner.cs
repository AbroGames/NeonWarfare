using NeonWarfare.Scenes.World.Features.Players;

namespace NeonWarfare.GameTests.World.Fixtures;

/// <summary>
/// Counts the reports instead of creating the UI.
/// </summary>
public class RecordingLocalPlayerOwner : ILocalPlayerOwner
{
    public int JoinedCount { get; private set; }

    // What the UI would do the moment it is created
    public Action? OnJoined { get; set; }

    public void Joined()
    {
        JoinedCount++;
        OnJoined?.Invoke();
    }
}
