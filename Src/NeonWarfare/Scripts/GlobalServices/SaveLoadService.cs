using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;
using KludgeBox.Logging;
using KludgeBox.Persistence;
using NeonWarfare.Scenes.Worlds.Ports;
using Serilog;
using FileAccess = Godot.FileAccess;

namespace NeonWarfare.Scripts.GlobalServices;

public class SaveLoadService : ISaveFiles
{
    
    public class SaveException(string message, Exception innerException = null) : Exception(message, innerException);
    public class LoadException(string message, Exception innerException = null) : Exception(message, innerException);
    public readonly record struct SaveFileInfo(SaveFileName FileName, ulong ModifiedTime);
    
    private const string DefaultSaveDirName = "saves";
    private const string SaveExtension = ".bin";
    private const string NewSaveNameFormat = "yyyy-MM-dd_HH-mm";

    private readonly ILogger _log = LogFactory.GetForStatic<SaveLoadService>();

    private readonly UserStorage _storage = new();
    private readonly string _saveDirName;

    /// <param name="saveDirName">The folder of the saves, relative to <c>user://</c>.</param>
    public SaveLoadService(string saveDirName = DefaultSaveDirName)
    {
        _saveDirName = saveDirName;
    }

    public SaveFileName GenNewSaveFileName()
    {
        return new SaveFileName(DateTime.Now.ToString(NewSaveNameFormat, CultureInfo.InvariantCulture));
    }
    
    public List<SaveFileInfo> GetAllSaveFiles()
    {
        string dirPath = _storage.GetPathForFile(_saveDirName);
        return DirAccess.GetFilesAt(dirPath)
            .Where(filename => filename.EndsWith(SaveExtension))
            .Select(filename => new SaveFileInfo(
                FileName: new SaveFileName(Path.GetFileNameWithoutExtension(filename)),
                ModifiedTime: FileAccess.GetModifiedTime(dirPath.PathJoin(filename))))
            .OrderByDescending(file => file.ModifiedTime)
            .ToList();
    }

    public bool CheckFileExists(SaveFileName saveFileName)
    {
        return _storage.FileExists(GetFileName(saveFileName));
    }
    
    public string GetFullPath(SaveFileName saveFileName)
    {
        return _storage.GetPathForFile(GetFileName(saveFileName));
    }

    /// <summary>
    /// The previous file of <paramref name="saveFileName"/> survives any failure, because the file is replaced
    /// atomically.
    /// </summary>
    /// <exception cref="SaveException">Nothing was saved.</exception>
    public void SaveToDisk(byte[] data, SaveFileName saveFileName)
    {
        string fileName = GetFileName(saveFileName);
        try
        {
            _storage.CreateDirectory(_saveDirName);
            _storage.WriteAllBytes(fileName, data);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new SaveException($"Failed to write '{fileName}': {e.Message}", e);
        }
        _log.Information("Successfully save file '{fileName}'", fileName);
    }
    
    /// <exception cref="LoadException">The file cannot be read.</exception>
    public byte[] LoadFromDisk(SaveFileName saveFileName)
    {
        string fileName = GetFileName(saveFileName);
        byte[] data;
        try
        {
            data = _storage.ReadAllBytes(fileName);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new LoadException($"Failed to read '{fileName}': {e.Message}", e);
        }
        _log.Information("Successfully load file '{fileName}'", fileName);
        return data;
    }

    private string GetFileName(SaveFileName saveFileName) => _saveDirName.PathJoin(saveFileName.Value + SaveExtension);
}
