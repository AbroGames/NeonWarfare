using System.Collections.Generic;
using System.Linq;
using KludgeBox.Persistence;

namespace NeonWarfare.Scripts.GlobalServices.KnownServers;

public class KnownServersService
{
    private const string FileName = "known-servers.json";

    private readonly JsonFile<List<KnownServer>> _file = new(FileName);

    private List<KnownServer> _servers;

    public void Init()
    {
        _servers = _file.ReadOrWriteDefault(KnownServer.IsValidList, () => []);
    }

    public List<KnownServer> GetAll()
    {
        return _servers;
    }

    public void Add(KnownServer server)
    {
        _servers.Add(server);
        _file.Write(_servers);
    }

    public void Remove(KnownServer server)
    {
        _servers.Remove(server);
        _file.Write(_servers);
    }

    public bool Exists(string host, int? port)
    {
        return _servers.Any(server => server.Host == host && server.Port == port);
    }
}
