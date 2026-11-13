using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using NeonWarfare.Scenes.World.Infra.Composition;

namespace NeonWarfare.Scenes.World.Features.Players;

[Query]
public class PlayerQuery(PlayersStorageQuery players, PlayersSessionStorageQuery session)
{
    public IEnumerable<PlayerModel> OnlinePlayers()
    {
        PlayersModel model = players.Model;
        return session.Model.OnlinePlayerUids.Select(uid => model.PlayerByUid[uid]);
    }

    // Throws rather than returning null, because otherwise every caller would need a null-check branch
    public PlayerModel Get(string uid)
    {
        if (!players.Model.PlayerByUid.TryGetValue(uid, out PlayerModel player))
        {
            throw new InvalidOperationException("There is no player with uid {0}".FormatWith(uid));
        }

        return player;
    }
}
