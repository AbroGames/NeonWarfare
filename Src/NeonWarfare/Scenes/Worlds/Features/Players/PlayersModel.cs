using System.Collections.Generic;
using NeonWarfare.Scripts;
using RepliCAT;

namespace NeonWarfare.Scenes.Worlds.Features.Players;

public class PlayersModel
{
    public IReadOnlyDictionary<PlayerUid, PlayerModel> PlayerByUid => _playerByUid;
    [Replicated] private readonly ReplicatedDictionary<PlayerUid, PlayerModel> _playerByUid = new();

    public PlayerModel AddPlayer(PlayerUid uid)
    {
        var player = new PlayerModel(uid);
        _playerByUid.Add(uid, player);
        return player;
    }
}