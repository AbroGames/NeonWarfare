using System.Collections.Generic;
using System.Linq;

namespace NeonWarfare.Scripts.GlobalServices.KnownServers;

public record KnownServer(string Host, int? Port, string Label)
{
    public static bool IsValidList(List<KnownServer> servers) =>
        servers.All(server => server != null && !string.IsNullOrWhiteSpace(server.Host));
}
