namespace NeonWarfare.Scripts.GlobalServices.ResumableGame;

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

    public static ResumableGame GetSingleplayer(SaveFileName saveName)
    {
        return GetNone() with
        {
            Type = ResumableType.RunSingleplayer,
            SaveName = saveName.Value,
        };
    }
        
    public static ResumableGame GetCreateServer(SaveFileName saveName, int port, bool isDedicated)
    {
        return GetNone() with
        {
            Type = ResumableType.CreateServer,
            SaveName = saveName.Value,
            Port = port,
            IsDedicated = isDedicated
        };
    }
        
    public bool IsValid() => Type switch
    {
        ResumableType.None => true,
        ResumableType.RunSingleplayer => SaveFileName.TryParse(SaveName, out _),
        ResumableType.CreateServer => SaveFileName.TryParse(SaveName, out _) && Port != null && IsDedicated != null,
        ResumableType.ConnectToServer => Host != null && Port != null,
        _ => false
    };

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