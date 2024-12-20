using System;
using System.Collections.Generic;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using NeonWarfare;

public partial class ServerGame
{
    public IReadOnlyDictionary<long, ServerPlayerProfile> PlayerProfiles => _playerProfiles;
    
    private readonly Dictionary<long, ServerPlayerProfile> _playerProfiles = new();

    public void AddPlayerProfile(long id)
    {
        if (!_playerProfiles.TryAdd(id, new ServerPlayerProfile(id)))
        {
            throw new ArgumentException($"Player with Id {id} already exists.");
        }
    }

    public void RemovePlayerProfile(long id)
    {
        _playerProfiles.Remove(id);
    }
}