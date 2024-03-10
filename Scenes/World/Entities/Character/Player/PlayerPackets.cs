using KludgeBox.Events;
using KludgeBox.Net;
using KludgeBox.Net.Packets;

namespace NeoVector;

[GamePacket]
public class ClientPositionPlayerPacket(long nid, double x, double y, double dir) : AbstractPacket
{
    public long Nid { get; set; } = nid;
    public double X { get; set; } = x;
    public double Y { get; set; } = y;
    public double Dir { get; set; } = dir;
}




