using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Entities;


namespace NeonWarfare.Scenes.World.Features.Players;

[Query]
public class PlayersSessionStorageQuery(IEntityFinder entityFinder)
{
    public PlayersSessionModel Model => entityFinder.GetSingle<PlayersSessionStorage>().Model;
}
