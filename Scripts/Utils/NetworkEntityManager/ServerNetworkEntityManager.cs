using System.Collections.Generic;
using Godot;

namespace NeonWarfare;

public class ServerNetworkEntityManager : NetworkEntityManager
{
    
    private long _nextNid = 0; 
    
    public void AddEntity(NetworkEntityComponent networkEntityComponent)
    {
        long nextNid = _nextNid++;
        NidToNetworkEntity.Add(nextNid, networkEntityComponent);
        networkEntityComponent.Nid = _nextNid;
    }
    
    public void AddEntity(Node node)
    {
        AddEntity(node.GetChild<NetworkEntityComponent>());
    }
}