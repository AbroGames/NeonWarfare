
namespace NeonWarfare.Scenes.Worlds.Ports;

/// <summary>
/// What owns the process of a server World: its <c>Game</c>. The World only reports; whether the process stops is the
/// owner's decision.
/// </summary>
public interface IServerOwner
{
    /// <summary>
    /// The player with the admin uid has disconnected; a displacement by its own new connection is no leave.
    /// Called inside the tick.
    /// </summary>
    void AdminLeft();
}
