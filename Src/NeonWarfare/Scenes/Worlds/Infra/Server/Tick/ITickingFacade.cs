namespace NeonWarfare.Scenes.Worlds.Infra.Server.Tick;

/// <summary>
/// A <c>[SimulationFacade]</c> with a time rule, ticked by <see cref="ServerTickLoop"/>.
/// It keeps no state of its own, so everything it decides on lives in the models and survives a save and a join.
/// It does not depend on the order of the other ticking facades, so logic that does lives in one facade.
/// </summary>
public interface ITickingFacade
{
    void Tick();
}
