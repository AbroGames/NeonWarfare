using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class ServerWorld 
{
    
    public IReadOnlyDictionary<long, ServerPlayer> PlayerById => _playerById;
    public IEnumerable<ServerPlayer> Players => _playerById.Values;
    
    private readonly Dictionary<long, ServerPlayer> _playerById = new();

    public void AddPlayer(ServerPlayer player)
    {
        if (_playerById.ContainsKey(player.PlayerProfile.Id)) 
        {
            throw new ArgumentException($"Player with Id {player.PlayerProfile.Id} already exists.");
        }

        _playerById[player.PlayerProfile.Id] = player;
        AddChild(player);
    }

    public void RemovePlayer(long id)
    {
        _playerById[id].QueueFree();
        _playerById.Remove(id);
    }
    
    public IReadOnlyDictionary<long, ServerPlayer> GetPlayersByIdExcluding(long excludeId)
    {
        return _playerById
            .Where(kv => kv.Key != excludeId)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }
    
    public IEnumerable<ServerPlayer> GetPlayersExcluding(long excludeId)
    {
        return GetPlayersByIdExcluding(excludeId).Values;
    }
}