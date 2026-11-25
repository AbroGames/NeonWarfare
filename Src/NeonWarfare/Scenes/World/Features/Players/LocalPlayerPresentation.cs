using NeonWarfare.Scenes.World.Infra.Composition;

namespace NeonWarfare.Scenes.World.Features.Players;

/// <summary>
/// "Me" for the UI. Who this process is, is the client's knowledge, not world state: it is in no model, save or
/// replication, and a dedicated server has no such service.
/// </summary>
[Presentation]
public class LocalPlayerPresentation(LocalPlayer localPlayer, PlayerQuery players)
{
    public string Uid => localPlayer.Uid;

    /// <returns><c>null</c> while the player is not online: before its join is applied or after it leaves.</returns>
    public PlayerModel TryGetPlayer() => players.TryGetOnline(Uid);
}
