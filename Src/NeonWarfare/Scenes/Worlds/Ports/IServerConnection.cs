using System;

namespace NeonWarfare.Scenes.Worlds.Ports;

/// <summary>
/// The connection from a client World to the server.
/// On the host the packet reaches its own World synchronously, see Docs/Networking.md#transport-and-roles.
/// </summary>
public interface IServerConnection
{
    /// <summary>
    /// Sends the packet reliably and in order.
    /// The bytes are valid only during the call, so an implementation that sends later must copy them.
    /// </summary>
    void Send(ReadOnlySpan<byte> packet);
}
