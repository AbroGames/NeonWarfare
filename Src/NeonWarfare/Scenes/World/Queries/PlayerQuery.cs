using System.Collections.Generic;
using System.Linq;
using NeonWarfare.Scenes.World.Composition;
using NeonWarfare.Scenes.World.Models;

namespace NeonWarfare.Scenes.World.Queries;

[Query]
public class PlayerQuery(PersistenceModel persistence, SessionModel session)
{
    public IEnumerable<PlayerModel> OnlinePlayers() =>
        session.OnlinePlayerUids.Select(uid => persistence.PlayerByUid[uid]);
}
