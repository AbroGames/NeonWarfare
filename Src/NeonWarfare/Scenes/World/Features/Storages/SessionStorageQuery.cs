using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Entities;


namespace NeonWarfare.Scenes.World.Features.Storages;

[Query]
public class SessionStorageQuery(IEntityFinder entityFinder)
{
    public SessionModel Model => entityFinder.GetSingle<SessionStorage>().Model;
}
