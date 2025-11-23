namespace NeonWarfare.Scripts.Service.CmdArgs;

//TODO Точно нужен сервис? Вроде как мы используем эти параметры только в Root Starters и всё (а дальше пробрасываем как свойства в Game). В идеале так и оставить.
//TODO Вообще-то ещё в LibInit.SetNodeNetworkExtensionsIsClientChecker(_ => !Service.CmdArgs.IsDedicatedServer). Но по сути тут не обязательно ссылаться именно на Service.CmdArgs
public class CmdArgsService : KludgeBox.Core.CmdArgsService 
{ 
    public bool IsDedicatedServer { get; private set; }
    public ClientArgs Client { get; private set; }
    public DedicatedServerArgs DedicatedServer { get; private set; }
    
    //TODO Общее свойство и для Client и для DedicatedServer
    public bool GodotLogPush { get; private set; }

    public CmdArgsService() 
    {
        IsDedicatedServer = ContainsInCmdArgs(DedicatedServerArgs.DedicatedServerFlag);

        if (IsDedicatedServer)
        {
            DedicatedServer = DedicatedServerArgs.GetFromCmd(this);
            GodotLogPush = DedicatedServer.GodotLogPush;
        }
        else
        {
            Client = ClientArgs.GetFromCmd(this);
            GodotLogPush = Client.GodotLogPush;
        }
    }
}