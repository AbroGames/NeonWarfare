using System;
using NeonWarfare.Scenes.Worlds.Infra.Client.Events;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Ports;

namespace NeonWarfare.Scenes.Worlds.Features.Players;

/// <summary>
/// The player of this process, for the UI.
/// Who this process is belongs to the client, not to the world state, so it is in no model, save or replication.
/// </summary>
[Presentation]
public class LocalPlayerPresentation(LocalPlayer localPlayer, ILocalPlayerOwner owner, PlayerQuery players)
    : IEventHandlerOwner
{
    public string Uid => localPlayer.Uid;

    /// <summary>
    /// Throws instead of returning <c>null</c>, because the UI exists only while the player is online, see
    /// <see cref="ILocalPlayerOwner"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Before the join is applied or after the leave.</exception>
    public PlayerModel Player =>
        players.TryGetOnline(Uid) ?? throw new InvalidOperationException($"The local player {Uid} is not online");

    [EventHandler]
    private void Handle(PlayerJoinedEvent e)
    {
        if (e.Uid == localPlayer.Uid) owner.Joined();
    }
}
