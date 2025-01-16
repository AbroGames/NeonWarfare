using System;

namespace NeonWarfare.Scripts.Utils.Networking.PacketBus.Listeners;

[Flags]
public enum ListenerSide
{
    None = 0b00,
    Client = 0b01,
    Server = 0b10,
    Both = Client | Server
}
