using System;
using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using KludgeBox.DI.Requests.SceneServiceInjection;
using NeonWarfare.Scenes.World.Data.PersistenceData;
using NeonWarfare.Scenes.World.Data.PersistenceData.Player;
using NeonWarfare.Scenes.World.Data.TemporaryData;
using NeonWarfare.Scenes.World.Service.Characters;
using NeonWarfare.Scenes.World.Service.DataSerializer;
using Serilog;

namespace NeonWarfare.Scenes.World.Service;

/// <summary>
/// Use for send player data (like uid, nick, color etc.) to server.<br/>
/// Use in multiplayer and singleplayer games for init player info.
/// </summary>
public partial class WorldSynchronizerService : Node
{
    private const int NicknameMinLength = 3;
    private const int NicknameMaxLength = 25;
    private const string UidAlreadyUsedErrorMessage = "Player with the same uid already online";
    private static readonly string LengthOfNicknameErrorMessage = $"Length of nickname must be between {NicknameMinLength} and {NicknameMaxLength} characters";
    private const string NicknameContainsSpaceErrorMessage = "Nickname contains space";
    private const string ColorValueErrorMessage = "Your color is too dark";
    
    public event Action SyncStartedOnClientEvent;
    public event Action SyncEndedOnClientEvent;
    public event Action<string> SyncRejectOnClientEvent;
    
    public event Action<int> SyncEndedOnServerEvent;

    [SceneService] private WorldPersistenceData _persistenceData;
    [SceneService] private WorldTemporaryData _temporaryData;
    [SceneService] private WorldDataSerializerService _dataSerializerService;
    [SceneService] private WorldPlayerService _playerService;
    [Logger] private ILogger _log;
    
    /// <summary>
    /// Hoster uid, or uid from cmd param in dedicated server.<br/>
    /// <c>Player.IsAdmin</c> in <c>WorldPersistenceData</c> for this player automatically will change to true.<br/>
    /// If next application start will be with <c>AdminUid = null</c>, then <c>Player.IsAdmin</c>
    /// in <c>WorldPersistenceData</c> still be true.<br/>
    /// </summary>
    private string _adminUid;

    public override void _Ready()
    {
        Di.Process(this);
    }

    public void InitOnServer(string adminUid = null)
    {
        _adminUid = adminUid;
    }

    public void StartSyncOnClient(string uid, string nick, Color color)
    {
        _log.Information("Starting sync with server as '{nick}' (uid: {uid})", nick, uid);
        SyncStartedOnClientEvent?.Invoke();
        NewClientInitOnServer(uid, nick, color);
    }

    private void NewClientInitOnServer(string uid, string nick, Color color) => RpcId(ServerId, MethodName.NewClientInitOnServerRpc, uid, nick, color);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)] 
    private void NewClientInitOnServerRpc(string uid, string nick, Color color)
    {
        int connectedClientId = GetMultiplayer().GetRemoteSenderId();
        _log.Information("Peer {peer} attempting to sync as '{nick}' (uid: {uid})", connectedClientId, nick, uid);
        
        if (_temporaryData.PlayerUidByPeerId.Values.Contains(uid))
        {
            _log.Warning("Syncing peer {peer} was rejected with error: {error}", connectedClientId, UidAlreadyUsedErrorMessage);
            RejectSyncOnClient(connectedClientId, UidAlreadyUsedErrorMessage);
        }
        if (nick.Length < NicknameMinLength || nick.Length > NicknameMaxLength)
        {
            _log.Warning("Syncing peer {peer} was rejected with error: {error}", connectedClientId, LengthOfNicknameErrorMessage);
            RejectSyncOnClient(connectedClientId, LengthOfNicknameErrorMessage);
        }
        if (nick.Contains(' '))
        {
            _log.Warning("Syncing peer {peer} was rejected with error: {error}", connectedClientId, NicknameContainsSpaceErrorMessage);
            RejectSyncOnClient(connectedClientId, NicknameContainsSpaceErrorMessage);
        }
        if (color.Luminance < 0.2)
        {
            _log.Warning("Syncing peer {peer} was rejected with error: {error}", connectedClientId, ColorValueErrorMessage);
            RejectSyncOnClient(connectedClientId, ColorValueErrorMessage);
        }
        
        _temporaryData.PlayerUidByPeerId.Add(connectedClientId, uid);

        if (!_persistenceData.Players.PlayerByUid.ContainsKey(uid))
        {
            _persistenceData.Players.AddPlayer(new PlayerData
            {
                Uid = uid,
                Nick = nick,
                Color = color
            });
        }
        PlayerData playerData = _persistenceData.Players.PlayerByUid[uid];
        playerData.IsAdmin |= uid.Equals(_adminUid);
        _log.Information("Player data from peer {peer} was synced", connectedClientId);
        
        _log.Information("Sending serialized world data to peer {peer}", connectedClientId);
        EndSyncOnClient(connectedClientId, _dataSerializerService.SerializeWorldData());
    }

    private void EndSyncOnClient(long peerId, byte[] serializableData) => RpcId(peerId, MethodName.EndSyncOnClientRpc, serializableData);
    [Rpc(CallLocal = true)]
    private void EndSyncOnClientRpc(byte[] serializableData)
    {
        _log.Information("Received serialized world data from server");
        _dataSerializerService.DeserializeWorldData(serializableData);
        EndSyncOnServer();
        _log.Information("Syncing complete successfully");
        
        SyncEndedOnClientEvent?.Invoke();
    }
    
    private void RejectSyncOnClient(long peerId, string errorMessage) => RpcId(peerId, MethodName.RejectSyncOnClientRpc, errorMessage);
    [Rpc(CallLocal = true)] 
    private void RejectSyncOnClientRpc(string errorMessage)
    {
        _log.Error("Syncing with the server was rejected with error: {error}", errorMessage);
        SyncRejectOnClientEvent?.Invoke(errorMessage);
    }
    
    private void EndSyncOnServer() => RpcId(ServerId, MethodName.EndSyncOnServerRpc);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)] 
    private void EndSyncOnServerRpc()
    {
        int connectedClientId = GetMultiplayer().GetRemoteSenderId();
        _log.Information("Syncing peer {peer} completed successfully", connectedClientId );
        
        _playerService.SpawnPlayer(connectedClientId);
        SyncEndedOnServerEvent?.Invoke(connectedClientId);
    }
}