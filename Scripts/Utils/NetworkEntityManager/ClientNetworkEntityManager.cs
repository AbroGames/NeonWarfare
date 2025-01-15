using System.Collections.Generic;
using Godot;

namespace NeonWarfare;

public class ClientNetworkEntityManager : NetworkEntityManager
{
    
    public void AddEntity(Node node, long nid)
    {
        NetworkEntityComponent networkEntityComponent = new NetworkEntityComponent(nid);
        node.AddChild(networkEntityComponent);
        NidToNetworkEntity.Add(networkEntityComponent.Nid, networkEntityComponent);
    }
}