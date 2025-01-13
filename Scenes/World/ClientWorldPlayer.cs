using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class ClientWorld
{
    
    public ClientMyPlayer MyPlayer { get; set; }
    
    public IReadOnlyDictionary<long, ClientPlayer> PlayerById => _playerById;
    public IEnumerable<ClientPlayer> Players => _playerById.Values;
    
    private readonly Dictionary<long, ClientPlayer> _playerById = new();
    
    public void AddMyPlayer(ClientMyPlayer player)
    {
        if (MyPlayer != null)
        {
            throw new ArgumentException("Player already exists.");
        }
        
        MyPlayer = player;
        AddChild(player);
    }

    public void RemoveMyPlayer()
    {
        MyPlayer.QueueFree();
        MyPlayer = null;
    }

    public void AddPlayer(ClientPlayer player)
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
    
    public IReadOnlyDictionary<long, ClientPlayer> GetPlayersByIdExcluding(long excludeId)
    {
        return _playerById
            .Where(kv => kv.Key != excludeId)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }
    
    public IEnumerable<ClientPlayer> GetPlayersExcluding(long excludeId)
    {
        return GetPlayersByIdExcluding(excludeId).Values;
    }
}