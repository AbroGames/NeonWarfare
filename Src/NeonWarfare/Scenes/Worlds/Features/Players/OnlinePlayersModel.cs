using RepliCAT;

namespace NeonWarfare.Scenes.Worlds.Features.Players;

/// <summary>
/// The uids of the players online now, not saved, see <see cref="OnlinePlayersStorage"/>.
/// </summary>
public class OnlinePlayersModel
{
    [Replicated] public readonly ReplicatedSet<string> Uids = new();
}