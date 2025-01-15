﻿using System;
using System.Collections.Generic;
using System.Linq;

public partial class ClientGame
{
    public ClientPlayerProfile PlayerProfile { get; private set; }
    
    public IReadOnlyDictionary<long, ClientAllyProfile> AllyProfilesByPeerId => _allyProfilesByPeerId;
    public IEnumerable<ClientAllyProfile> AllyProfiles => _allyProfilesByPeerId.Values;
    
    private readonly Dictionary<long, ClientAllyProfile> _allyProfilesByPeerId = new();

    public void AddPlayerProfile(long peerId)
    {
        if (PlayerProfile != null)
        {
            throw new ArgumentException("Player already exists.");
        }
        
        PlayerProfile = new ClientPlayerProfile(peerId);
        _allyProfilesByPeerId.Add(peerId, PlayerProfile);
    }
    
    public void AddAllyProfile(long peerId) //TODO вызывать в OnPeerConnect, на клиенте пока не реализован этот слушатель. В дисконекте игрока (на клиенте и на сервере) уничтожать PlayerProfile
    {
        if (!_allyProfilesByPeerId.TryAdd(peerId, new ClientAllyProfile(peerId)))
        {
            throw new ArgumentException($"Ally with PeerId {peerId} already exists.");
        }
    }

    public void RemoveAllyProfile(long peerId)
    {
        _allyProfilesByPeerId.Remove(peerId);
    }
    
    public IReadOnlyDictionary<long, ClientAllyProfile> GetAllyProfilesByPeerIdExcluding(long excludePeerId)
    {
        return _allyProfilesByPeerId
            .Where(kv => kv.Key != excludePeerId)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }
    
    public IEnumerable<ClientAllyProfile> GetAllyProfilesExcluding(long excludePeerId)
    {
        return GetAllyProfilesByPeerIdExcluding(excludePeerId).Values;
    }
    
    public IEnumerable<ClientAllyProfile> GetAllyProfilesExcludePlayer()
    {
        return GetAllyProfilesExcluding(PlayerProfile.PeerId);
    }
}