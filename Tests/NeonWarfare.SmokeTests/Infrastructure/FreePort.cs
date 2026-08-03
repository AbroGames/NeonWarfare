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
    /// Asks the OS for a free port from the dynamic range by binding to port 0 and reading back what
    /// was assigned. The port is released immediately, so this is a hint rather than a reservation —
    /// good enough here, and the alternative (a hardcoded port) collides far more often.
    /// </summary>
    public static int Take()
    {
        TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        finally
        {
            listener.Stop();
        }
    }
}
