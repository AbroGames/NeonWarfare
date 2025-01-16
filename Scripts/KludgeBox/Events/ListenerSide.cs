using System;

namespace NeonWarfare.Scripts.KludgeBox.Events;

[Flags]
public enum ListenerSide
{
    Server = 0b01,
    Client = 0b10,
    Both = Server | Client
}
