using System;
using System.Collections.Generic;
using System.Linq;

public partial class ClientGame
{
    public ClientPlayerProfile PlayerProfile { get; private set; }
    
    public IReadOnlyDictionary<long, ClientAllyProfile> AllyProfilesById => _allyProfilesById;
    public IEnumerable<ClientAllyProfile> AllyProfiles => _allyProfilesById.Values;
    
    private readonly Dictionary<long, ClientAllyProfile> _allyProfilesById = new();

    public void AddPlayerProfile(long id)
    {
        if (PlayerProfile != null)
        {
            throw new ArgumentException("Player already exists.");
        }
        
        PlayerProfile = new ClientPlayerProfile(id);
        _allyProfilesById.Add(id, PlayerProfile);
    }
    
    public void AddAllyProfile(long id) //TODO вызывать в OnPeerConnect, на клиенте пока не реализован этот слушатель. В дисконекте игрока (на клиенте и на сервере) уничтожать PlayerProfile
    {
        if (!_allyProfilesById.TryAdd(id, new ClientAllyProfile(id)))
        {
            throw new ArgumentException($"Ally with Id {id} already exists.");
        }
    }

    public void RemoveAllyProfile(long id)
    {
        _allyProfilesById.Remove(id);
    }
    
    public IReadOnlyDictionary<long, ClientAllyProfile> GetAllyProfilesByIdExcluding(long excludeId)
    {
        return _allyProfilesById
            .Where(kv => kv.Key != excludeId)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }
    
    public IEnumerable<ClientAllyProfile> GetAllyProfilesExcluding(long excludeId)
    {
        return GetAllyProfilesByIdExcluding(excludeId).Values;
    }
    
    public IEnumerable<ClientAllyProfile> GetAllyProfilesExcludePlayer()
    {
        return GetAllyProfilesExcluding(PlayerProfile.Id);
    }
}