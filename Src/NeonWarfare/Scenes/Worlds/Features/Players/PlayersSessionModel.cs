using RepliCAT;

namespace NeonWarfare.Scenes.World.Features.Players;

/// <summary>
/// Not saved: empty after a load until the players join again, see <see cref="PlayersSessionStorage"/>.
/// </summary>
public class PlayersSessionModel
{
    [Replicated] public readonly ReplicatedSet<string> OnlinePlayerUids = new();
}