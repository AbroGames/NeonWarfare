using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using NeonWarfare;

public partial class ClientGame
{
    public ClientPlayerProfile MyPlayerProfile { get; private set; }
    
    public IReadOnlyDictionary<long, ClientPlayerProfile> PlayerProfilesById => _playerProfilesById;
    public IEnumerable<ClientPlayerProfile> PlayerProfiles => _playerProfilesById.Values;
    
    private readonly Dictionary<long, ClientPlayerProfile> _playerProfilesById = new();

    public void AddMyPlayerProfile(long id)
    {
        if (MyPlayerProfile != null)
        {
            throw new ArgumentException("Player already exists.");
        }
        
        MyPlayerProfile = new ClientPlayerProfile(id);
    }
    
    public void AddPlayerProfile(long id) //TODO вызывать в OnPeerConnect, на клиенте пока не реализован этот слушатель. В дисконекте игрока (на клиенте и на сервере) уничтожать PlayerProfile
    {
        if (!_playerProfilesById.TryAdd(id, new ClientPlayerProfile(id)))
        {
            throw new ArgumentException($"Player with Id {id} already exists.");
        }
    }

    public void RemovePlayerProfile(long id)
    {
        _playerProfilesById.Remove(id);
    }
    
    public IReadOnlyDictionary<long, ClientPlayerProfile> GetPlayerProfilesByIdExcluding(long excludeId)
    {
        return _playerProfilesById
            .Where(kv => kv.Key != excludeId)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }
    
    public IEnumerable<ClientPlayerProfile> GetPlayerProfilesExcluding(long excludeId)
    {
        return GetPlayerProfilesByIdExcluding(excludeId).Values;
    }
}