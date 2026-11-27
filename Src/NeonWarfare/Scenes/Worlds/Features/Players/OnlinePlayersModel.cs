using RepliCAT;

namespace NeonWarfare.Scenes.Worlds.Features.Players;

/// <summary>
/// Not saved: empty after a load until the players join again, see <see cref="OnlinePlayersStorage"/>.
/// </summary>
public class OnlinePlayersModel
{
    [Replicated] public readonly ReplicatedSet<string> Uids = new();
}