using System;
using System.Text;
using Godot;
using KludgeBox.VFS;
using Environment = System.Environment;

namespace NeonWarfare.Scripts.KludgeBox.Loggers;

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

    public void Warning(object msg = null, Exception exception = null)
    {
        Print(msg, exception);
    }

    public void Error(object msg = null, Exception exception = null)
    {
        Print(msg, exception);
    }

    public void Critical(object msg = null, Exception exception = null)
    {
        Print(msg, exception);
    }

    private void Print(object msg, Exception exception = null)
    {
        if(msg is null && exception is null)
        {
            _file.StoreString("\n");
            return;
        }
        
        var sb = new StringBuilder();
        sb.Append(msg ?? "");
        sb.Append(msg is null || exception is null ? "" : "\n");
        sb.Append(exception?.ToString() ?? "");
        
        _file.StoreString($"{sb}\n");
    }
}
