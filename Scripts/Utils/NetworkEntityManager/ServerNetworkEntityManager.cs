using System.Collections.Generic;
using Godot;

namespace NeonWarfare;

public class ServerNetworkEntityManager : NetworkEntityManager
{
    
    private long _nextNid = 0; 
    
    public void AddEntity(Node node)
    {
        NetworkEntityComponent networkEntityComponent = new NetworkEntityComponent(_nextNid++);
        node.AddChild(networkEntityComponent);
        NidToNetworkEntity.Add(networkEntityComponent.Nid, networkEntityComponent);
    }
}