using System;

namespace NeonWarfare.Scenes.World.Infra.Protocol;

/// <summary>
/// A message read from the network is unknown, not allowed or broken. The packet is dropped.
/// </summary>
public class NetMessageFormatException : Exception
{
    public NetMessageFormatException(string message) : base(message) { }

    public NetMessageFormatException(string message, Exception innerException) : base(message, innerException) { }
}
