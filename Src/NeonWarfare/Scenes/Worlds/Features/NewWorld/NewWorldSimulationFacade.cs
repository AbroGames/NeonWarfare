using NeonWarfare.Scenes.World.Features.Players;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Entities;


namespace NeonWarfare.Scenes.World.Features.NewWorld;

/// <summary>
/// Spawns what a world has from its start. Only a new world: a loaded one gets these entities from the save, a
/// client from the world snapshot.
/// </summary>
[SimulationFacade]
public class NewWorldSimulationFacade(EntitySpawner spawner)
{
    public void Create()
    {
        spawner.SpawnOnRoot<PlayersStorage>();
        spawner.SpawnOnRoot<PlayersSessionStorage>();
    }
}
