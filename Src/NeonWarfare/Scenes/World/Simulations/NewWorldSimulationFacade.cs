using NeonWarfare.Scenes.World.Composition;
using NeonWarfare.Scenes.World.Entities;
using NeonWarfare.Scenes.World.Entities.Storages;

namespace NeonWarfare.Scenes.World.Simulations;

/// <summary>
/// Spawns what a world has from its start. Only a new world: a loaded one gets these entities from the save, a
/// client from the world snapshot.
/// </summary>
[SimulationFacade]
public class NewWorldSimulationFacade(EntitySpawner spawner, WorldPackedScenes scenes)
{
    public void Create()
    {
        spawner.SpawnOnRoot<PersistenceStorage>(scenes.PersistenceStorage);
        spawner.SpawnOnRoot<SessionStorage>(scenes.SessionStorage);
    }
}
