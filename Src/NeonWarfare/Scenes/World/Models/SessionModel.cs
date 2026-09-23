using RepliCAT;

namespace NeonWarfare.Scenes.World.Models;

/// <summary>
/// Session-scoped storage cleared when the game ends.
/// </summary>
public class SessionModel
{
    /// <summary>
    /// List of current connected players.
    /// </summary>
    [Replicated] public readonly ReplicatedDictionary<int, string> PlayerUidByPeerId = new();
}