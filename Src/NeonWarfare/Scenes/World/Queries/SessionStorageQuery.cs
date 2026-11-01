using NeonWarfare.Scenes.World.Composition;
using NeonWarfare.Scenes.World.Entities;
using NeonWarfare.Scenes.World.Entities.Storages;
using NeonWarfare.Scenes.World.Models;

namespace NeonWarfare.Scenes.World.Queries;

[Query]
public class SessionStorageQuery(IEntityFinder entityFinder)
{
    public SessionModel Model => entityFinder.GetSingle<SessionStorage>().Model;
}
