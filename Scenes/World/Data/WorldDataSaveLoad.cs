using System;
using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using Serilog;

namespace NeonWarfare.Scenes.World.Data;

public class WorldDataSaveLoad
{

    private const string SaveDirPath = "user://saves/";
    private const string SaveExtension = ".bin";
    private const string AutoSaveName = "auto";
    
    private WorldPersistenceData _worldData;
    [Logger] private ILogger _log;

    public WorldDataSaveLoad(WorldPersistenceData worldData) 
    {
        Di.Process(this);
        _worldData = worldData;
    }

    public bool Save(string saveFileName)
    {
        _worldData.General.GeneralData.SaveFileName = saveFileName;
        return SaveToDisk(_worldData.Serializer.SerializeWorldData(), saveFileName);
    }
    
    public bool AutoSave()
    {
        return SaveToDisk(_worldData.Serializer.SerializeWorldData(), AutoSaveName);
    }

    public bool Load(string saveFileName)
    {
        byte[] data = LoadFromDisk(saveFileName);
        if (data == null) return false;
        
        try
        {
            _worldData.Serializer.DeserializeWorldData(data);
        }
        catch (Exception e)
        {
            //TODO Вместо возвращаемого bool мб выбрасывать ошибку выше, чтобы обрабатывать потом в Host MpGame и в Host Single Game?
            _log.Error($"Failed to deserialize world data from save file '{SaveDirPath + saveFileName + SaveExtension}': {e.Message}");
            return false;
        }

        return true;
    }

    private bool SaveToDisk(byte[] data, string saveFileName)
    {
        DirAccess.MakeDirRecursiveAbsolute(SaveDirPath);
        using var file = FileAccess.Open(SaveDirPath + saveFileName + SaveExtension, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            _log.Error($"Failed to save file '{SaveDirPath + saveFileName + SaveExtension}': {FileAccess.GetOpenError()}");
            return false;
        }
        
        file.StoreBuffer(data);
        file.Close();
        return true;
    }
    
    private byte[] LoadFromDisk(string saveFileName)
    {
        using var file = FileAccess.Open(SaveDirPath + saveFileName + SaveExtension, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            _log.Error($"Failed to load file '{SaveDirPath + saveFileName + SaveExtension}': {FileAccess.GetOpenError()}");
            return null;
        }
        
        byte[] data = file.GetBuffer((long) file.GetLength());
        file.Close();
        
        return data;
    }
}