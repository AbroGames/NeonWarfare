using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using Serilog;

namespace NeonWarfare.Scripts.Service;

public class SaveLoadService
{
    
    public class SaveException(string message, Exception innerException = null) : Exception(message, innerException);
    public class LoadException(string message, Exception innerException = null) : Exception(message, innerException);
    
    public readonly string SaveDirPath = "user://saves/";
    public readonly string SaveExtension = ".bin";
    public readonly string AutoSaveName = "auto";

    [Logger] ILogger _log;
    
    public SaveLoadService()
    {
        Di.Process(this);
    }
    
    public List<string> GetAllSaveFiles()
    {
        return DirAccess.GetFilesAt(SaveDirPath)
            .Where(filename => filename.EndsWith(SaveExtension))
            .Select(filename => (
                Name: filename,
                Size: FileAccess.GetModifiedTime(SaveDirPath + filename)))
            .OrderByDescending(file => file.Size)
            .Select(file => System.IO.Path.GetFileNameWithoutExtension(file.Name))
            .ToList();
    }

    public bool CheckFileExists(string saveFileName)
    {
        string fullPath = GetFullPath(saveFileName);
        return FileAccess.FileExists(fullPath);
    }
    
    public string GetFullPath(string saveFileName)
    {
        return SaveDirPath + saveFileName + SaveExtension;
    }
    
    public void SaveToDisk(byte[] data, string saveFileName)
    {
        DirAccess.MakeDirRecursiveAbsolute(SaveDirPath);
        string fullPath = GetFullPath(saveFileName);
        using var file = FileAccess.Open(fullPath, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            _log.Error("Failed to save file '{fullPath}': {error}", fullPath, FileAccess.GetOpenError());
            throw new SaveException($"Failed to save file '{fullPath}': {FileAccess.GetOpenError()}");
        }
        
        file.StoreBuffer(data);
        file.Close();
        _log.Information("Successfully save file '{fullPath}'", fullPath);
    }
    
    public byte[] LoadFromDisk(string saveFileName)
    {
        string fullPath = GetFullPath(saveFileName);
        using var file = FileAccess.Open(fullPath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            _log.Error("Failed to load file '{fullPath}': {error}", fullPath, FileAccess.GetOpenError());
            throw new LoadException($"Failed to load file '{fullPath}': {FileAccess.GetOpenError()}");
        }

        byte[] data = file.GetBuffer((long) file.GetLength());
        file.Close();
        if (data == null)
        {
            _log.Error("Failed to load data from file '{fullPath}'", fullPath);
            throw new LoadException($"Failed to load data from file '{fullPath}'");
        }
        _log.Information("Successfully load file '{fullPath}'", fullPath);
        
        return data;
    }
}