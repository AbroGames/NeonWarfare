using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Entities;

namespace NeonWarfare.Scenes.Worlds.Features.Options;

[Query]
public class WorldOptionsQuery(IEntityFinder entityFinder)
{
    public WorldOptions Options => entityFinder.GetSingle<WorldOptionsStorage>().Options;
}
