using System.Collections.Generic;
using System.Collections.ObjectModel;
using Godot;
using MessagePack;
using NeonWarfare.Scenes.World.Service.DataSerializer;
using static MessagePack.MessagePackSerializer;

namespace NeonWarfare.Scenes.World.Data.PersistenceData.Player;

public partial class PlayerDataStorage : Node, ISerializableStorage
{

    [MessagePackObject]
    public record InnerStorage
    {
        [Key(0)] public Dictionary<string, PlayerData> PlayerByUid = new();
    }
    
    public IReadOnlyDictionary<string, PlayerData> PlayerByUid => new ReadOnlyDictionary<string, PlayerData>(_innerStorage.PlayerByUid);
    private InnerStorage _innerStorage = new();

    public void AddPlayer(PlayerData player)
    {
        AddPlayerLocal(player);
        Rpc(MethodName.AddPlayerRpc, Serialize(player));
    }
    
    [Rpc(CallLocal = false)]
    private void AddPlayerRpc(byte[] playerBytes) => AddPlayerLocal(Deserialize<PlayerData>(playerBytes));

    private void AddPlayerLocal(PlayerData player)
    {
        _innerStorage.PlayerByUid[player.Uid] = player;
        SetPropertyListener(player);
    }

    private void SetPropertyListener(PlayerData player)
    {
        player.PropertyChanged += (p, _) => UpdatePlayer((PlayerData) p);
    }
    
    public void RemovePlayer(PlayerData player) => RemovePlayer(player.Uid);
    public void RemovePlayer(string uid) => Rpc(MethodName.RemovePlayerRpc, uid);
    [Rpc(CallLocal = true)]
    private void RemovePlayerRpc(string uid)
    {
        _innerStorage.PlayerByUid.Remove(uid);
    }
    
    private void UpdatePlayer(PlayerData player) => Rpc(MethodName.UpdatePlayerRpc, Serialize(player));
    [Rpc(CallLocal = false)]
    private void UpdatePlayerRpc(byte[] playerBytes)
    {
        PlayerData player = Deserialize<PlayerData>(playerBytes);
        AddPlayerLocal(player);
    }
    
    public byte[] SerializeStorage()
    {
        return Serialize(_innerStorage);
    }

    public void DeserializeStorage(byte[] storageBytes)
    {
        _innerStorage = Deserialize<InnerStorage>(storageBytes);
    }
    
    public void SetAllPropertyListeners()
    {
        foreach (PlayerData player in _innerStorage.PlayerByUid.Values) SetPropertyListener(player);
    }
}