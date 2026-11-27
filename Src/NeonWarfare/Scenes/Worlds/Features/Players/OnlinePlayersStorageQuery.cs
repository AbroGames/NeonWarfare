using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Entities;


namespace NeonWarfare.Scenes.Worlds.Features.Players;

[Query]
public class OnlinePlayersStorageQuery(IEntityFinder entityFinder)
{
    public OnlinePlayersModel Model => entityFinder.GetSingle<OnlinePlayersStorage>().Model;
}
