using System;
using Humanizer;
using NeonWarfare.Scenes.World.Infra.ClientNetwork;
using NeonWarfare.Scenes.World.Infra.Composition;

namespace NeonWarfare.Scenes.World.Features.Players;

/// <summary>
/// "Me" for the UI. Who this process is, is the client's knowledge, not world state: it is in no model, save or
/// replication, and a dedicated server has no such service.
/// </summary>
[Presentation]
public class LocalPlayerPresentation(LocalPlayer localPlayer, ILocalPlayerOwner owner, PlayerQuery players)
{
    private const string NotOnlineError = "The local player {0} is not online";

    public string Uid => localPlayer.Uid;

    /// <summary>
    /// Throws rather than returning <c>null</c>: the UI is created only once the player is online, see
    /// <see cref="ILocalPlayerOwner"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Before the join is applied or after the leave.</exception>
    public PlayerModel Player =>
        players.TryGetOnline(Uid) ?? throw new InvalidOperationException(NotOnlineError.FormatWith(Uid));

    [EventHandler]
    private void Handle(PlayerJoinedEvent e)
    {
        if (e.Uid == localPlayer.Uid) owner.Joined();
    }
}
