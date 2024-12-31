using System;

namespace NeonWarfare.Utils.Networking;

[Flags]
public enum ListenerSide
{
    None = 0b00,
    Client = 0b01,
    Server = 0b10,
    Both = Client | Server
}