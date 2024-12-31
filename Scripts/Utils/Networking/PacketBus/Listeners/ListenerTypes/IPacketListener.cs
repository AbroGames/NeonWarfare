using System;

namespace NeonWarfare.Utils.Networking.DestinationTypes;

public interface IPacketListener
{
    Type PacketType { get; }
    bool IsActive { get; }
    
    bool CanAccept(IPacket packet);
    void Deliver(IPacket packet);
    
    void Cancel();
}