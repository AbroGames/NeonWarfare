using NeonWarfare.Scenes.Worlds.Infra.Client.Events;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scripts;
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
    public PlayerUid Uid => localPlayer.Uid;
    public PlayerModel Player => players.Get(Uid);

    [EventHandler]
    private void Handle(PlayerJoinedEvent e)
    {
        if (e.Uid == localPlayer.Uid) owner.Joined();
    }
}
