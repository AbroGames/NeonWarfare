using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Ports;

namespace NeonWarfare.GameTests.Worlds.Fixtures;

/// <summary>
/// Records the reports instead of creating the UI.
/// </summary>
public class RecordingLocalPlayerOwner : ILocalPlayerOwner
{
    public int JoinedCount { get; private set; }

    public List<DisconnectReason> Disconnections { get; } = [];

    // What the UI would do the moment it is created
    public Action? OnJoined { get; set; }

    public void Joined()
    {
        JoinedCount++;
        OnJoined?.Invoke();
    }

    public void Disconnected(DisconnectReason reason) => Disconnections.Add(reason);
}
