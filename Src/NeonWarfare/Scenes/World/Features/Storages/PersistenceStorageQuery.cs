using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Entities;


namespace NeonWarfare.Scenes.World.Features.Storages;

[Query]
public class PersistenceStorageQuery(IEntityFinder entityFinder)
{
    public PersistenceModel Model => entityFinder.GetSingle<PersistenceStorage>().Model;
}
