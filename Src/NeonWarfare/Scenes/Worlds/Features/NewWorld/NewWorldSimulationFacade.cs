using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Entities;


namespace NeonWarfare.Scenes.Worlds.Features.NewWorld;

/// <summary>
/// Spawns what a new world has from its start.
/// A loaded world gets these entities from the save, and a client from the join snapshot.
/// </summary>
[SimulationFacade]
public class NewWorldSimulationFacade(EntitySpawner spawner)
{
    public void Create()
    {
        spawner.SpawnOnRoot<PlayersStorage>();
        spawner.SpawnOnRoot<OnlinePlayersStorage>();
    }
}
