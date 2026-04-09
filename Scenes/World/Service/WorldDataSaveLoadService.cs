using System;
using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using KludgeBox.DI.Requests.SceneServiceInjection;
using NeonWarfare.Scenes.World.Data.PersistenceData;
using NeonWarfare.Scenes.World.Service.DataSerializer;
using NeonWarfare.Scripts.Service;
using Serilog;

namespace NeonWarfare.Scenes.World.Service;

public partial class WorldDataSaveLoadService : Node
{
    
    private const string SaveFilenameMustBeNotEmptyErrorMessage = "Filename for saving must be not empty";
    private const string NotRightsForSaveErrorMessage = "You don't have the rights for saving";

    public event Action<string> SaveRejectedClientEvent;
    public event Action<string> SaveSuccessServerEvent;
    
    [SceneService] private WorldPersistenceData _persistenceData;
    [SceneService] private WorldDataSerializerService _serializerService;
    [SceneService] private WorldFacadeService _facadeService;
    [Logger] private ILogger _log;

    public override void _Ready()
    {
        Di.Process(this);
    }
    
    public void Save(string saveFileName) => RpcId(ServerId, MethodName.SaveRpc, saveFileName);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void SaveRpc(string saveFileName)
    {
        int peerId = GetMultiplayer().GetRemoteSenderId();
        if (!_facadeService.IsAdmin(peerId))
        {
            SaveReject(peerId, NotRightsForSaveErrorMessage);
            return;
        }

        if (String.IsNullOrEmpty(saveFileName))
        {
            SaveReject(peerId, SaveFilenameMustBeNotEmptyErrorMessage);
            return;
        }

        string currentSaveFileName = _persistenceData.General.GeneralData.SaveFileName;
        try
        {
            _persistenceData.General.GeneralData.SaveFileName = saveFileName;
            byte[] data = TrySerializeWorldData();
            Services.SaveLoad.SaveToDisk(data, saveFileName);
            SaveSuccessServerEvent?.Invoke(saveFileName);
        }
        catch (SaveLoadService.SaveException saveException)
        {
            // Return old SaveFileName, because we couldn't save the game with new filename.
            _persistenceData.General.GeneralData.SaveFileName = currentSaveFileName;
            SaveReject(peerId, saveException.Message);
        }
    }

    public void SaveReject(long peerId, string errorMessage) => RpcId(peerId, MethodName.SaveRejectRpc, errorMessage);
    [Rpc(CallLocal = true)]
    private void SaveRejectRpc(string errorMessage)
    {
        SaveRejectedClientEvent?.Invoke(errorMessage);
    }
    
    public void AutoSave()
    {
        byte[] data = TrySerializeWorldData();
        Services.SaveLoad.SaveToDisk(data, Services.SaveLoad.AutoSaveName);
    }

    public void Load(string saveFileName)
    {
        byte[] data = Services.SaveLoad.LoadFromDisk(saveFileName);
        TryDeserializeWorldData(data);
    }
    
    private byte[] TrySerializeWorldData()
    {
        try
        {
            return _serializerService.SerializeWorldData();
        }
        catch (Exception e)
        {
            _log.Error("Failed to serialize world data: {error}", e.Message);
            throw new SaveLoadService.SaveException($"Failed to serialize world data: {e.Message}", e);
        }
    }

    private void TryDeserializeWorldData(byte[] worldDataBytes)
    {
        try
        {
            _serializerService.DeserializeWorldData(worldDataBytes);
        }
        catch (Exception e)
        {
            _log.Error("Failed to deserialize world data: {error}", e.Message);
            throw new SaveLoadService.LoadException($"Failed to deserialize world data: {e.Message}", e);
        }
    }
}