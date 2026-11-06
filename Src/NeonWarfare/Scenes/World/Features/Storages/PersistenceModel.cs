using System.Collections.Generic;
using NeonWarfare.Scenes.World.Features.Players;
using RepliCAT;

namespace NeonWarfare.Scenes.World.Features.Storages;

public class PersistenceModel
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