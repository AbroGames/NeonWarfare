namespace NeonWarfare.Scenes.Worlds.Infra.Tick;

/// <summary>
/// The read side of the tick counter, open to every layer. Each configuration has exactly one implementation, written
/// only by its own layer.
/// </summary>
public interface IWorldClock
{
    /// <summary>
    /// The number of the current or the last tick: N from the start of tick N until tick N + 1 starts.
    /// </summary>
    long CurrentTick { get; }
}
