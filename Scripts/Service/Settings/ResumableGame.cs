namespace GodotTemplate.Scripts.Service.Settings;

public record ResumableGame(
    ResumableGame.ResumableType Type,
    string SaveName,
    string Host,
    int? Port,
    bool? IsDedicated
)
{
    public enum ResumableType
    {
        None,
        RunSingleplayer,
        CreateServer,
        ConnectToServer,
    }
        
    public static ResumableGame GetNone()
    {
        return new ResumableGame(
            Type: ResumableType.None,
            SaveName: null,
            Host: null,
            Port: null,
            IsDedicated: null);
    }

    public static ResumableGame GetSingleplayer(string saveName)
    {
        return GetNone() with
        {
            Type = ResumableType.RunSingleplayer,
            SaveName = saveName,
        };
    }
        
    public static ResumableGame GetCreateServer(string saveName, int port, bool isDedicated)
    {
        return GetNone() with
        {
            Type = ResumableType.CreateServer,
            SaveName = saveName,
            Port = port,
            IsDedicated = isDedicated
        };
    }
        
    public static ResumableGame GetConnectToServer(string host, int port)
    {
        return GetNone() with
        {
            Type = ResumableType.ConnectToServer,
            Host = host,
            Port = port
        };
    }
}