using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using NeonWarfare;

public partial class ServerGame
{
    public IReadOnlyDictionary<long, ServerPlayerProfile> PlayerProfilesById => _playerProfilesById;
    public IEnumerable<ServerPlayerProfile> PlayerProfiles => _playerProfilesById.Values;
    
    private readonly Dictionary<long, ServerPlayerProfile> _playerProfilesById = new();

    public void AddPlayerProfile(long id)
    {
        if (!_playerProfilesById.TryAdd(id, new ServerPlayerProfile(id)))
        {
            throw new ArgumentException($"Player with Id {id} already exists.");
        }
    }

    public void RemovePlayerProfile(long id)
    {
        _playerProfilesById.Remove(id);
    }
    
    public IReadOnlyDictionary<long, ServerPlayerProfile> GetPlayerProfilesByIdExcluding(long excludeId)
    {
        return _playerProfilesById
            .Where(kv => kv.Key != excludeId)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }
    
    public IEnumerable<ServerPlayerProfile> GetPlayerProfilesExcluding(long excludeId)
    {
        return GetPlayerProfilesByIdExcluding(excludeId).Values;
    }

}