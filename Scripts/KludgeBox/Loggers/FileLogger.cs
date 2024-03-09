using System;
using Godot;
using KludgeBox.VFS;
using Environment = System.Environment;

namespace KludgeBox.Loggers;

public class FileLogger : ILogger
{
    private GodotFileSystem _fileSystem;
    private FsDirectory _logsDir;
    private FsFile _logFile;

    private DateTime _startTime;
    private int _pid;
    
    private FileAccess _file;
    public FileLogger(string logsDir = "/logs")
    {
        _fileSystem = new GodotFileSystem(GodotFsRoot.User, logsDir, true);
        _logsDir = _fileSystem.Root;
        _startTime = DateTime.Now;
        _pid = Environment.ProcessId;
        _logFile = _logsDir.CreateFile($"Game-{_startTime:yyyy-MM-dd-HH-mm-ss}-{_pid:D6}.log");

        _file = FileAccess.Open(_logFile.RealPath, FileAccess.ModeFlags.ReadWrite);
    }
    
    public void Debug(object msg = null)
    {
        Print(msg);
    }

    public void Info(object msg = null)
    {
        Print(msg);
    }

    public void Warning(object msg = null)
    {
        Print(msg);
    }

    public void Error(object msg = null)
    {
        Print(msg);
    }

    public void Critical(object msg = null)
    {
        Print(msg);
    }

    private void Print(object msg)
    {
        if(msg is null)
        {
            _file.StoreString("\n");
            //_logFile.AppendText("\n");
            return;
        }
        _file.StoreString($"{msg}\n");
        //_logFile.AppendText($"{msg}\n");
    }
}