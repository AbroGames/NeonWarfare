using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class ServerWorld 
{
    
    public IReadOnlyDictionary<long, ServerPlayer> PlayerByPeerId => _playerByPeerId;
    public IEnumerable<ServerPlayer> Players => _playerByPeerId.Values;
    
    private readonly Dictionary<long, ServerPlayer> _playerByPeerId = new();

    public void AddEntity(ServerEnemy enemy)
    {
        //TODO реализовать хранилище enemy и способ добавления. Аналогично и для других обхектов (стены, например).
        //TODO Надо бы иметь общий пул всех объектов (для синхронизации, например). И дополнительные пересекающиеся пулы characters, игроков, врагов и т.п.
        //TODO Но ведь уже есть GetChild<> ??? Может пулы вообще не нужны? Достаточно какого-нибудь автовычисляемого свойства ? Маппинг по nid уже есть, маппинг по peerId тоже есть 
        AddChild(enemy);
    }
    
    public void AddPlayer(ServerPlayer player)
    {
        if (_playerByPeerId.ContainsKey(player.PlayerProfile.PeerId)) 
        {
            throw new ArgumentException($"Player with PeerId {player.PlayerProfile.PeerId} already exists.");
        }

        _playerByPeerId[player.PlayerProfile.PeerId] = player;
        AddChild(player);
    }

    public void RemovePlayer(long peerId)
    {
        _playerByPeerId[peerId].QueueFree();
        _playerByPeerId.Remove(peerId);
    }
    
    public void RemovePlayer(ServerPlayer serverPlayer)
    {
        RemovePlayer(serverPlayer.PlayerProfile.PeerId);
    }
    
    public IReadOnlyDictionary<long, ServerPlayer> GetPlayersByPeerIdExcluding(long excludePeerId)
    {
        return _playerByPeerId
            .Where(kv => kv.Key != excludePeerId)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }
    
    public IEnumerable<ServerPlayer> GetPlayersExcluding(long excludePeerId)
    {
        return GetPlayersByPeerIdExcluding(excludePeerId).Values;
    }
}