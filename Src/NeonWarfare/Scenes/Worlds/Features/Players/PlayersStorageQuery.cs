using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Entities;


namespace NeonWarfare.Scenes.Worlds.Features.Players;

[Query]
public class PlayersStorageQuery(IEntityFinder entityFinder)
{
    public PlayersModel Model => entityFinder.GetSingle<PlayersStorage>().Model;
}
