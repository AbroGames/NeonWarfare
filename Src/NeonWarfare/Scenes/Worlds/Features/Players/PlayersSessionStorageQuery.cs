using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Entities;


namespace NeonWarfare.Scenes.Worlds.Features.Players;

[Query]
public class PlayersSessionStorageQuery(IEntityFinder entityFinder)
{
    public PlayersSessionModel Model => entityFinder.GetSingle<PlayersSessionStorage>().Model;
}
