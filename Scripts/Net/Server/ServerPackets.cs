using KludgeBox.Events;
using KludgeBox.Net;
using KludgeBox.Net.Packets;

namespace NeoVector;

[GamePacket]
public class ServerWaitBattleEndPacket : AbstractPacket;

[GamePacket]
public class ServerChangeWorldPacket(ServerChangeWorldPacket.ServerWorldType worldType) : AbstractPacket
{
    
    public enum ServerWorldType { Unknown, Safe, Battle };
    public ServerWorldType WorldType { get; set; } = worldType;
}

[GamePacket]
public class ServerSpawnPlayerPacket(double x, double y, double dir) : AbstractPacket
{
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
    public double Dir { get; set; } = dir;
}




