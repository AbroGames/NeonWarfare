using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Entities;


namespace NeonWarfare.Scenes.World.Features.Players;

[Query]
public class PlayersStorageQuery(IEntityFinder entityFinder)
{
    public PlayersModel Model => entityFinder.GetSingle<PlayersStorage>().Model;
}
