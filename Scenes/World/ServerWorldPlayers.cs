using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox;

namespace NeonWarfare;

public abstract partial class ServerWorld : Node2D
{
    
    public IReadOnlyDictionary<long, Player> PlayerById => _playerById;
    public IEnumerable<Player> Players => _playerById.Values; //TODO ServerPlayer
    
    private readonly Dictionary<long, Player> _playerById = new();

    public Player CreateAndAddPlayer(ServerPlayerProfile playerProfile)
    {
        if (_playerById.ContainsKey(playerProfile.Id)) 
        {
            throw new ArgumentException($"Player with Id {playerProfile.Id} already exists.");
        }

        Player player = ServerRoot.Instance.PackedScenes.Player.Instantiate<Player>(); //TODO ServerPlayer special ~~constructor~~ static builder, based on playerProfile
        _playerById[playerProfile.Id] = player;
        AddChild(player);
        return player;
    }

    public void RemovePlayer(long id)
    {
        _playerById[id].QueueFree();
        _playerById.Remove(id);
    }
    
    public IReadOnlyDictionary<long, Player> GetPlayersByIdExcluding(long excludeId)
    {
        return _playerById
            .Where(kv => kv.Key != excludeId)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }
    
    public IEnumerable<Player> GetPlayersExcluding(long excludeId)
    {
        return GetPlayersByIdExcluding(excludeId).Values;
    }
}