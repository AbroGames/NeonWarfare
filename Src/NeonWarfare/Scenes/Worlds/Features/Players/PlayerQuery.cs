using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using NeonWarfare.Scenes.Worlds.Infra.Composition;

namespace NeonWarfare.Scenes.Worlds.Features.Players;

[Query]
public class PlayerQuery(PlayersStorageQuery players, OnlinePlayersStorageQuery onlinePlayers)
{
    public IEnumerable<PlayerModel> OnlinePlayers()
    {
        PlayersModel model = players.Model;
        return onlinePlayers.Model.Uids.Select(uid => model.PlayerByUid[uid]);
    }

    public PlayerModel TryGetOnline(string uid) =>
        onlinePlayers.Model.Uids.Contains(uid) ? players.Model.PlayerByUid[uid] : null;

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
