using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using Serilog;

namespace NeonWarfare.Scripts.Service;

public class SaveLoadService
{
    
    public class SaveException(string message, Exception innerException = null) : Exception(message, innerException);
    public class LoadException(string message, Exception innerException = null) : Exception(message, innerException);
    public readonly record struct SaveFileInfo(string FileName, ulong ModifiedTime);
    
    public readonly string SaveDirPath = "user://saves/";
    public readonly string SaveExtension = ".bin";
    public readonly string NewSaveNameFormat = "yyyy-MM-dd_HH:mm";

    [Logger] ILogger _log;
    
    public SaveLoadService()
    {
        Di.Process(this);
    }

    public string GenNewSaveFileName()
    {
        return DateTime.Now.ToString(NewSaveNameFormat, CultureInfo.InvariantCulture);
    }
    
    public List<SaveFileInfo> GetAllSaveFiles()
    {
        return DirAccess.GetFilesAt(SaveDirPath)
            .Where(filename => filename.EndsWith(SaveExtension))
            .Select(filename => new SaveFileInfo(
                FileName: System.IO.Path.GetFileNameWithoutExtension(filename),
                ModifiedTime: FileAccess.GetModifiedTime(SaveDirPath + filename)))
            .OrderByDescending(file => file.ModifiedTime)
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