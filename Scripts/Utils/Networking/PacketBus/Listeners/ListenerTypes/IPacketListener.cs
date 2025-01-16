using System;
using NeonWarfare.Scripts.Utils.Networking.PacketBus.PacketTypes;

namespace NeonWarfare.Scripts.Utils.Networking.PacketBus.Listeners.ListenerTypes;

public interface IPacketListener
{
    Type PacketType { get; }
    bool IsActive { get; }
    
    bool CanAccept(IPacket packet);
    void Deliver(IPacket packet);
    
    void Cancel();
}
