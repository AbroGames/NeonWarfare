using System.Collections.Generic;
using System.Linq;
using NeonWarfare.Scenes.World.Composition;
using NeonWarfare.Scenes.World.Models;

namespace NeonWarfare.Scenes.World.Queries;

[Query]
public class PlayerQuery(PersistenceStorageQuery persistence, SessionStorageQuery session)
{
    public IEnumerable<PlayerModel> OnlinePlayers()
    {
        PersistenceModel model = persistence.Model;
        return session.Model.OnlinePlayerUids.Select(uid => model.PlayerByUid[uid]);
    }
}
