using RepliCAT;

namespace NeonWarfare.Scenes.World.Models;

/// <summary>
/// Session-scoped storage cleared when the game ends.
/// </summary>
public class SessionModel
{
    [Replicated] public readonly ReplicatedSet<string> OnlinePlayerUids = new();
}