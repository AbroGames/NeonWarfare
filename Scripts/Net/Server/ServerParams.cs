namespace NeoVector;

public readonly record struct ServerParams(int Port, string AdminNickname, int? ParentPid)
{
    public static readonly string ServerFlag = "--server";
    public static readonly string HeadlessFlag = "--headless";
    public static readonly string RenderFlag = "--render";
    public static readonly string PortParam = "--port";
    public static readonly string AdminParam = "--admin";
    public static readonly string ParentPidParam = "--parent-pid";
}