namespace NeonWarfare.Scenes.Worlds.Features.Players;

/// <summary>
/// What shows this process's player its World, owned by <c>Game</c>. The World only reports; the UI is created by
/// the owner, so that it never exists while the player is not online.
/// </summary>
public interface ILocalPlayerOwner
{
    /// <summary>
    /// The server has applied this process's join, and the player is online. Called on the host inside the tick, on
    /// a remote client from the events packet of its join tick.
    /// </summary>
    void Joined();
}
