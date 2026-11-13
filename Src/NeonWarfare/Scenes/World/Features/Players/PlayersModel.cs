using System.Collections.Generic;
using RepliCAT;

namespace NeonWarfare.Scenes.World.Features.Players;

public class PlayersModel
{
    public IReadOnlyDictionary<string, PlayerModel> PlayerByUid => _playerByUid;
    [Replicated] private readonly ReplicatedDictionary<string, PlayerModel> _playerByUid = new();

    public PlayerModel AddPlayer(string uid)
    {
        var player = new PlayerModel(uid);
        _playerByUid.Add(uid, player);
        return player;
    }
}