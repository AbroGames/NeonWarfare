using System;
using System.Collections.Generic;
using System.Linq;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Entities;

namespace NeonWarfare.Scenes.Worlds.Features.Players;

[Query]
public class PlayerQuery(IEntityFinder entityFinder)
{
    public PlayersModel PlayersModel => entityFinder.GetSingle<PlayersStorage>().Model;

    public OnlinePlayersModel OnlineModel => entityFinder.GetSingle<OnlinePlayersStorage>().Model;

    public IEnumerable<PlayerModel> GetOnlinePlayers()
    {
        PlayersModel players = PlayersModel;
        return OnlineModel.Uids.Select(uid => players.PlayerByUid[uid]);
    }

    public PlayerModel TryGetOnline(string uid) =>
        OnlineModel.Uids.Contains(uid) ? PlayersModel.PlayerByUid[uid] : null;

    /// <summary>Throws instead of returning <c>null</c>, so a caller needs no null check.</summary>
    public PlayerModel Get(string uid)
    {
        if (!PlayersModel.PlayerByUid.TryGetValue(uid, out PlayerModel player))
        {
            throw new InvalidOperationException($"There is no player with uid {uid}");
        }

        return player;
    }
}
