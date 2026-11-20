
namespace NeonWarfare.Scenes.Worlds.Ports;

/// <summary>
/// What owns the process of a server World, owned by its starter. The World only reports; whether the process stops
/// is the owner's decision.
/// </summary>
public interface IServerOwner
{
    /// <summary>
    /// The player with the admin uid has left. Called inside the tick.
    /// </summary>
    void AdminLeft();
}
