using NeonWarfare.Scenes.World.Composition;
using NeonWarfare.Scenes.World.Entities;
using NeonWarfare.Scenes.World.Entities.Storages;
using NeonWarfare.Scenes.World.Models;

namespace NeonWarfare.Scenes.World.Queries;

[Query]
public class PersistenceStorageQuery(IEntityFinder entityFinder)
{
    public PersistenceModel Model => entityFinder.GetSingle<PersistenceStorage>().Model;
}
