using System.Collections.Generic;
using System.Linq;
using NeonWarfare.Scenes.World.Features.Storages;
using NeonWarfare.Scenes.World.Infra.Composition;

namespace NeonWarfare.Scenes.World.Features.Players;

[Query]
public class PlayerQuery(PersistenceStorageQuery persistence, SessionStorageQuery session)
{
    public IEnumerable<PlayerModel> OnlinePlayers()
    {
        PersistenceModel model = persistence.Model;
        return session.Model.OnlinePlayerUids.Select(uid => model.PlayerByUid[uid]);
    }
}
