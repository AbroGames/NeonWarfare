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
    public IReadOnlyDictionary<long, ServerPlayerProfile> PlayerProfilesByPeerId => _playerProfilesByPeerId;
    public IEnumerable<ServerPlayerProfile> PlayerProfiles => _playerProfilesByPeerId.Values;
    
    private readonly Dictionary<long, ServerPlayerProfile> _playerProfilesByPeerId = new();

    public void AddPlayerProfile(long peerId)
    {
        if (!_playerProfilesByPeerId.TryAdd(peerId, new ServerPlayerProfile(peerId)))
        {
            throw new ArgumentException($"Player with PeerId {peerId} already exists.");
        }
    }

    public void RemovePlayerProfile(long peerId)
    {
        _playerProfilesByPeerId.Remove(peerId);
    }
    
    public IReadOnlyDictionary<long, ServerPlayerProfile> GetPlayerProfilesByPeerIdExcluding(long excludePeerId)
    {
        return _playerProfilesByPeerId
            .Where(kv => kv.Key != excludePeerId)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }
    
    public IEnumerable<ServerPlayerProfile> GetPlayerProfilesExcluding(long excludePeerId)
    {
        return GetPlayerProfilesByPeerIdExcluding(excludePeerId).Values;
    }

}