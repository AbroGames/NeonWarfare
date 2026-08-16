using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

namespace NeonWarfare.Scripts.Service.KnownServers;

public class KnownServersService
{
    private const string KnownServersPath = "user://known-servers.json";

    private List<KnownServer> _servers;

    public void Init()
    {
        _servers = new List<KnownServer>();
        Load();
    }

    public List<KnownServer> GetAll()
    {
        return _servers;
    }

    public void Add(KnownServer server)
    {
        _servers.Add(server);
        Save();
    }

    public void Remove(KnownServer server)
    {
        _servers.Remove(server);
        Save();
    }

    public bool Exists(string host, int? port)
    {
        return _servers.Any(server => server.Host == host && server.Port == port);
    }

    private void Save()
    {
        using var file = FileAccess.Open(KnownServersPath, FileAccess.ModeFlags.Write);
        string json = JsonSerializer.Serialize(GetAll());
        file.StoreString(json);
        file.Close();
    }

    private void Load()
    {
        if (!FileAccess.FileExists(KnownServersPath))
        {
            Save();
            return;
        }

        using var file = FileAccess.Open(KnownServersPath, FileAccess.ModeFlags.Read);
        string json = file.GetAsText();
        file.Close();

        _servers = JsonSerializer.Deserialize<List<KnownServer>>(json);
    }
}
