
namespace NeonWarfare.Scenes.Worlds.Ports;

/// <summary>
/// The owner of a server World's process, implemented by <c>Game</c>.
/// The World only reports, and the owner decides whether the process stops.
/// </summary>
public interface IServerOwner
{
    /// <summary>
    /// The player with the admin uid has disconnected. Called inside the tick.
    /// A displacement by the admin's own new connection is not a leave and does not call it.
    /// </summary>
    void AdminLeft();
}
