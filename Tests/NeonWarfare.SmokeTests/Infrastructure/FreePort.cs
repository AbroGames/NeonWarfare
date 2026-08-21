using System.Net;
using System.Net.Sockets;

namespace NeonWarfare.SmokeTests.Infrastructure;

/// <summary>
/// Picks a port for a scenario that hosts a server.
/// The default 25566 is deliberately avoided: a developer often has a server running from Rider, and
/// a clash would show up as "Failed to start server" rather than as an honest failure.
/// </summary>
public static class FreePort
{
    /// <summary>
    /// Asks the OS for a free UDP port from the dynamic range by binding to port 0 and reading back what
    /// was assigned. UDP, not TCP: the server is an ENetMultiplayerPeer, and a port free for one protocol
    /// can be taken for the other. The port is released immediately, so this is a hint rather than a
    /// reservation — good enough here, and the alternative (a hardcoded port) collides far more often.
    /// </summary>
    public static int Take()
    {
        using Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Bind(new IPEndPoint(IPAddress.Any, 0));
        return ((IPEndPoint)socket.LocalEndPoint!).Port;
    }
}
