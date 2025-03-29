using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace NeonWarfare.Scenes.Screen.MainMenuInterfaces.ConnectToServerInterface;

public class ServersStorage
{
    public string LastConnectionString { get; set; } = "";
    public int MaxConnections { get; set; } = 20;
    
    
    [JsonProperty]
    private List<string> _serversHistory { get; set; } = new();
    
    
    public IReadOnlyList<string> ServersHistory => _serversHistory;

    public void AddServer(string server)
    {
        var connectionsCount = _serversHistory.Count;
        
        _serversHistory = _serversHistory
            .TakeLast(MaxConnections)
            .Append(server)
            .ToList();
    }
}