using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Ports;
using Serilog;
using FileAccess = Godot.FileAccess;

namespace NeonWarfare.Scripts.GlobalServices;

public class SaveLoadService : ISaveFiles
{
    
    public class SaveException(string message, Exception innerException = null) : Exception(message, innerException);
    public class LoadException(string message, Exception innerException = null) : Exception(message, innerException);
    public readonly record struct SaveFileInfo(string FileName, ulong ModifiedTime);
    
    private const string DefaultSaveDirPath = "user://saves/";
    private const string SaveExtension = ".bin";
    private const string TempSuffix = ".tmp";
    private const string BackupSuffix = ".backup";
    private const string NewSaveNameFormat = "yyyy-MM-dd_HH-mm";

    private readonly ILogger _log = LogFactory.GetForStatic<SaveLoadService>();

    private readonly string _saveDirPath;
    private bool _isDedicatedServer;

    public SaveLoadService(string saveDirPath = DefaultSaveDirPath)
    {
        _saveDirPath = saveDirPath;
    }

    /// <summary>
    /// Before anything reads the save folder, method try to restore its state: a process killed in the middle
    /// of <see cref="SaveToDisk"/> leaves the save only as its backup, and a temporary file nobody will finish.
    /// </summary>
    public void Init(bool isDedicatedServer)
    {
        _isDedicatedServer = isDedicatedServer;
        if (!DirAccess.DirExistsAbsolute(_saveDirPath)) return;

        foreach (string fileName in DirAccess.GetFilesAt(_saveDirPath))
        {
            string path = _saveDirPath + fileName;
            if (fileName.EndsWith(SaveExtension + BackupSuffix))
            {
                RestoreFromBackup(path[..^BackupSuffix.Length]);
            }
            else if (fileName.EndsWith(SaveExtension + TempSuffix))
            {
                RemoveTemporary(path);
            }
        }
    }

    public bool IsAutoSaveEnabled()
    {
        return _isDedicatedServer
            ? Services.DedicatedServerSettings.GetSettings().AutoSaveEnabled
            : Services.GameSettings.GetSettings().AutoSaveEnabled;
    }

    public string GenNewSaveFileName()
    {
        return DateTime.Now.ToString(NewSaveNameFormat, CultureInfo.InvariantCulture);
    }
    
    public List<SaveFileInfo> GetAllSaveFiles()
    {
        return DirAccess.GetFilesAt(_saveDirPath)
            .Where(filename => filename.EndsWith(SaveExtension))
            .Select(filename => new SaveFileInfo(
                FileName: Path.GetFileNameWithoutExtension(filename),
                ModifiedTime: FileAccess.GetModifiedTime(_saveDirPath + filename)))
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
        return _saveDirPath + saveFileName + SaveExtension;
    }

    /// <summary>
    /// The previous file of <paramref name="saveFileName"/> survives any failure: the data goes to a temporary file
    /// first, and the old file is replaced only once the new one is complete.
    /// </summary>
    /// <exception cref="SaveException">Nothing was saved.</exception>
    public void SaveToDisk(byte[] data, string saveFileName)
    {
        DirAccess.MakeDirRecursiveAbsolute(_saveDirPath);
        string fullPath = GetFullPath(saveFileName);
        string tempPath = fullPath + TempSuffix;
        string backupPath = fullPath + BackupSuffix;

        try
        {
            WriteFile(tempPath, data);
        }
        catch
        {
            DirAccess.RemoveAbsolute(tempPath);
            throw;
        }

        bool hadPrevious = FileAccess.FileExists(fullPath);
        if (hadPrevious)
        {
            // Left by a complete save that could not remove it: the file itself is the newer one
            if (FileAccess.FileExists(backupPath)) DirAccess.RemoveAbsolute(backupPath);
            Error backupError = DirAccess.RenameAbsolute(fullPath, backupPath);
            if (backupError != Error.Ok)
            {
                DirAccess.RemoveAbsolute(tempPath);
                throw new SaveException($"Failed to back up '{fullPath}': {backupError}");
            }
        }

        Error renameError = DirAccess.RenameAbsolute(tempPath, fullPath);
        if (renameError != Error.Ok)
        {
            DirAccess.RemoveAbsolute(tempPath);
            if (hadPrevious) DirAccess.RenameAbsolute(backupPath, fullPath);
            throw new SaveException($"Failed to move '{tempPath}' to '{fullPath}': {renameError}");
        }

        // The save is complete either way: a stale backup is dropped by the next save
        if (hadPrevious && DirAccess.RemoveAbsolute(backupPath) != Error.Ok)
        {
            _log.Warning("Failed to remove the backup '{backupPath}'", backupPath);
        }
        _log.Information("Successfully save file '{fullPath}'", fullPath);
    }

    // StoreBuffer reports a failed write only through its result
    private void WriteFile(string path, byte[] data)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            throw new SaveException($"Failed to open '{path}': {FileAccess.GetOpenError()}");
        }
        if (!file.StoreBuffer(data) || file.GetError() != Error.Ok)
        {
            throw new SaveException($"Failed to write '{path}': {file.GetError()}");
        }
        file.Close();
    }

    // Save could be interrupted, if process was killed between moving the old file aside and moving the new one
    private void RestoreFromBackup(string fullPath)
    {
        string backupPath = fullPath + BackupSuffix;
        if (FileAccess.FileExists(fullPath)) return;

        Error error = DirAccess.RenameAbsolute(backupPath, fullPath);
        if (error == Error.Ok)
        {
            _log.Warning("Restored '{fullPath}' from its backup", fullPath);
        }
        else
        {
            _log.Error("Failed to restore '{fullPath}' from its backup: {error}", fullPath, error);
        }
    }

    private void RemoveTemporary(string path)
    {
        Error error = DirAccess.RemoveAbsolute(path);
        if (error == Error.Ok)
        {
            _log.Warning("Removed the unfinished save '{path}'", path);
        }
        else
        {
            _log.Error("Failed to remove the unfinished save '{path}': {error}", path, error);
        }
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